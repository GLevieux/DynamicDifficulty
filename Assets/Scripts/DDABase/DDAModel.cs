using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDAModel
{

    //Settings Data
    public string PlayerId;
    public string ChallengeId;
    DDADataManager DataManager;

    //Log reg model
    LogisticRegression.ModelLR LogReg;
    const double LRMinimalAccuracy = 0.6;
    public double LRAccuracy = 0;
    float LRExplo = 0.05f;
    bool LRAccuracyUpToDate = false;
    const int LRNbLastAttemptsToConsider = 150;

    //PMDelta model
    bool PMInitialized = false;
    double PMLastTheta = 0;
    bool PMWonLastTime = false;
    double PMDeltaValue = 0.1;
    float PMDeltaExploMin = 0.5f;
    float PMDeltaExploMax = 1.0f;

    public enum DDAAlgorithm
    {
        DDA_LOGREG, //Utilise la regression logistique (si modèle calibré, sinon PM_DELTA)
        DDA_PMDELTA, //Si on gagne, theta monte, si on perds, theta descend
        DDA_RANDOM_THETA, //Choisit un theta random
        DDA_RANDOM_LOGREG //Choisit une diff random et en déduit le theta avec la logreg (sinon on fait random theta)
    };

    DDAAlgorithm Algorithm = DDAAlgorithm.DDA_LOGREG;

    public enum DDALogRegError
    {
        OK,
        NOT_ENOUGH_SAMPLES,
        NOT_ENOUGH_WINS,
        NOT_ENOUGH_FAILS,
        NEWTON_RAPHSON_ERROR,
        ACCURACY_TOO_LOW,
        SUM_ERROR_TOO_HIGH,
        SD_PRED_TOO_LOW,
        SUM_ERROR_IS_NAN,
        SD_PRED_IS_NAN
    };

    public struct DiffParams
    {
        public double TargetDiff;
        public double TargetDiffWithExplo;
        public bool LogRegReady;
        public DDALogRegError LogRegError;
        public double LRAccuracy;
        public double Theta;
        public int NbAttemptsUsedToCompute;
        public DDAAlgorithm AlgorithmActuallyUsed;
        public DDAAlgorithm AlgorithmWanted;
        public double[] Betas;
    }

    /**
     * One can only create a model for a specific challenge and player, and with a chosen data mgmt strategy
     */
    public DDAModel(DDADataManager dataManager, string playerId, string challengeId)
    {
        DataManager = dataManager;
        PlayerId = playerId;
        ChallengeId = challengeId;
    }

    /**
     * Allows to specify which difficulty adaptation algorithm we want the model to use (see enum description)
     */
    public void setDdaAlgorithm(DDAAlgorithm algorithm)
    {
        Algorithm = algorithm;
    }

    /**
     * Permet de déterminer un point de départ. L'algo pmdelta
     * va partir de la pour augmenter ou diminuer la difficulté
     */
    public void setPMInit(double lastTheta, bool wonLastTime = false)
    {
        PMLastTheta = lastTheta;
        PMWonLastTime = wonLastTime;
        PMInitialized = true;
    }

    /**
     * Add new attempt to data and set is as last attempt
     */
    public void addLastAttempt(DDADataManager.Attempt attempt)
    {
        DataManager.addAttempt(PlayerId, ChallengeId, attempt);
        LRAccuracyUpToDate = false;
        PMWonLastTime = attempt.Result > 0;
        PMLastTheta = attempt.Thetas[0];
    }

    /**
     * Get gameplay parameter value for desired target difficulty
     * uses PMDeltaLastTheta for PMDelta algorithm
     */
    public DiffParams computeNewDiffParams(double targetDifficulty, bool doNotUpdateLRAccuracy = false)
    {
        DiffParams diffParams = new DiffParams();
        diffParams.LogRegReady = true;
        diffParams.AlgorithmWanted = Algorithm;
        diffParams.Betas = null;
        diffParams.LogRegError = DDALogRegError.OK;

        //Loading data
        List<DDADataManager.Attempt> attempts = DataManager.getAttempts(PlayerId, ChallengeId, LRNbLastAttemptsToConsider);

        //Data translation for LR
        LogisticRegression.DataLR data = new LogisticRegression.DataLR();
        List<double[]> indepVars = new List<double[]>();
        List<double> depVars = new List<double>();
        foreach (DDADataManager.Attempt attempt in attempts)
        {
            indepVars.Add(attempt.Thetas);
            depVars.Add(attempt.Result);
        }
        data.LoadDataFromList(indepVars, depVars);

        //On met a jour le dernier theta en fonction des datas si on ne l'a pas deja set
        if (indepVars.Count > 0 && !PMInitialized)
        {
            PMLastTheta = indepVars[indepVars.Count - 1][0];
            PMWonLastTime = depVars[depVars.Count - 1] > 0 ? true : false;
            PMInitialized = true;
        }

        //Check if enough data to update LogReg
        if (attempts.Count < 10)
        {
            Debug.Log("Less than 10 attempts, can not use LogReg prediciton");
            diffParams.LogRegReady = false;
            diffParams.LogRegError = DDALogRegError.NOT_ENOUGH_SAMPLES;
        }
        else
        {
            //Chekcing wins and fails
            double nbFail = 0;
            double nbWin = 0;
            foreach (DDADataManager.Attempt attempt in attempts)
            {
                if (attempt.Result == 0)
                    nbFail++;
                else
                    nbWin++;
            }

            //If only three fails or three wins
            if (nbFail <= 3 || nbWin <= 3)
            {
                Debug.Log("Less than 4 wins or 4 fails, will not use LogReg");
                diffParams.LogRegReady = false;

                if (nbWin <= 3)
                    diffParams.LogRegError = DDALogRegError.NOT_ENOUGH_WINS;
                if (nbFail <= 3)
                    diffParams.LogRegError = DDALogRegError.NOT_ENOUGH_FAILS;
            }
        }

        if (diffParams.LogRegReady)
        {
            //Debug.Log("Using " + data.DepVar.Length + " lines to update model");

            if (!doNotUpdateLRAccuracy && !LRAccuracyUpToDate)
            {
                //Ten fold cross val
                LRAccuracy = 0;

                for (int i = 0; i < 10; i++)
                {
                    double AccuracyNow = 0;
                    data = data.shuffle();
                    int nk = 10;
                    for (int k = 0; k < nk; k++)
                    {
                        LogisticRegression.DataLR dataTrain;
                        LogisticRegression.DataLR dataTest;
                        data.split(k * (100 / nk), (k + 1) * (100 / nk), out dataTrain, out dataTest);
                        LogReg = LogisticRegression.ComputeModel(dataTrain);
                        AccuracyNow += LogisticRegression.TestModel(LogReg, dataTest);
                    }
                    AccuracyNow /= nk;
                    LRAccuracy += AccuracyNow;
                }
                LRAccuracy /= 10;

                LRAccuracyUpToDate = true;

                //Using all data to update model
                LogReg = LogisticRegression.ComputeModel(data);
                diffParams.NbAttemptsUsedToCompute = data.DepVar.Length;
            }
            else
            {
                data = data.shuffle();
                LogReg = LogisticRegression.ComputeModel(data);
                diffParams.NbAttemptsUsedToCompute = data.DepVar.Length;
            }

            if (LRAccuracy < LRMinimalAccuracy)
            {
                Debug.Log("LogReg accuracy is under " + LRMinimalAccuracy + ", not using LogReg");
                diffParams.LogRegReady = false;
                diffParams.LogRegError = DDALogRegError.ACCURACY_TOO_LOW;
            }

            if (!LogReg.isUsable())
            {
                LRAccuracy = 0;
                diffParams.LogRegError = DDALogRegError.NEWTON_RAPHSON_ERROR;
            }
            else if (diffParams.LogRegReady)
            {   
                //Verifying if LogReg is ok : must be able to work in both ways 
                double errorSum = 0;
                double diffTest = 0.1;
                double[] pars = new double[1];
                double[] parsForAllDiff = new double[10];
                string res = "";
                for (int i = 0; i < 8; i++)
                {
                    pars[0] = LogReg.InvPredict(diffTest, pars, 0); //on regarde que la première variable.
                    parsForAllDiff[i] = pars[0];
                    res = "D = " + diffTest + " par = " + pars[0];
                    errorSum += System.Math.Abs(diffTest - LogReg.Predict(pars)); //On passe dans les deux sens on doit avoir pareil
                    res += " res = " + LogReg.Predict(pars) + "\n";
                    diffTest += 0.1;
                    //Debug.Log(res);
                }

                if (errorSum > 1 || double.IsNaN(errorSum))
                {
                    Debug.Log("Model is not solid, error = " + errorSum);
                    LRAccuracy = 0;
                    if (errorSum > 1)
                        diffParams.LogRegError = DDALogRegError.SUM_ERROR_TOO_HIGH;
                    if (double.IsNaN(errorSum))
                        diffParams.LogRegError = DDALogRegError.SUM_ERROR_IS_NAN;
                }

                //Verifying if LogReg is ok : sd of diff predictions in all theta range must not be 0
                double mean = 0;
                for (int i = 0; i < 8; i++)
                    mean += parsForAllDiff[i];
                mean /= 8;
                double sd = 0;
                for (int i = 0; i < 8; i++)
                    sd += (parsForAllDiff[i] - mean) * (parsForAllDiff[i] - mean);
                sd = System.Math.Sqrt(sd);

                //Debug.Log("Model parameter estimation sd = " + sd);

                if (sd < 0.05 || double.IsNaN(sd))
                {
                    Debug.Log("Model parameter estimation is always the same : sd=" + sd);
                    LRAccuracy = 0;

                    if (sd < 0.05)
                        diffParams.LogRegError = DDALogRegError.SD_PRED_TOO_LOW;
                    if (double.IsNaN(sd))
                        diffParams.LogRegError = DDALogRegError.SD_PRED_IS_NAN;
                }
            }
        }

        //Saving params
        diffParams.TargetDiff = targetDifficulty;
        diffParams.LRAccuracy = LRAccuracy;

        //Determining theta

        //If we want pmdelta or we want log reg but it's not available
        if ((Algorithm == DDAAlgorithm.DDA_LOGREG && !diffParams.LogRegReady) ||
             Algorithm == DDAAlgorithm.DDA_PMDELTA)
        {
            double delta = PMWonLastTime ? PMDeltaValue : -PMDeltaValue;
            delta *= Random.Range(PMDeltaExploMin, PMDeltaExploMax);
            diffParams.Theta = PMLastTheta + delta;
            diffParams.AlgorithmActuallyUsed = DDAAlgorithm.DDA_PMDELTA;

            //If regression is okay, we can tell the difficulty for this theta
            if (diffParams.LogRegReady)
            {
                double[] pars = new double[1];
                pars[0] = diffParams.Theta;
                diffParams.TargetDiff = 1.0 - LogReg.Predict(pars);
                diffParams.TargetDiffWithExplo = diffParams.TargetDiff;
            }
            else //Otherwise we just can tell we aim for 0.5
            {
                diffParams.TargetDiffWithExplo = 0.5;
                diffParams.TargetDiff = 0.5;
            }
        }

        //if we want log reg and it's available
        if (Algorithm == DDAAlgorithm.DDA_LOGREG && diffParams.LogRegReady)
        {
            diffParams.TargetDiffWithExplo = targetDifficulty + Random.Range(-LRExplo, LRExplo);
            diffParams.TargetDiffWithExplo = System.Math.Min(1.0, System.Math.Max(0, diffParams.TargetDiffWithExplo));
            diffParams.Theta = LogReg.InvPredict(1.0 - diffParams.TargetDiffWithExplo);
            diffParams.AlgorithmActuallyUsed = DDAAlgorithm.DDA_LOGREG;
        }

        //if we want random log reg and it's available
        if (Algorithm == DDAAlgorithm.DDA_RANDOM_LOGREG && diffParams.LogRegReady)
        {
            diffParams.TargetDiff = Random.Range(0.0f, 1.0f);
            diffParams.TargetDiffWithExplo = diffParams.TargetDiff; //Pas d'explo on est en random
            diffParams.Theta = LogReg.InvPredict(1.0 - diffParams.TargetDiffWithExplo);
            diffParams.AlgorithmActuallyUsed = DDAAlgorithm.DDA_RANDOM_LOGREG;
        }

        //If we want random
        if (Algorithm == DDAAlgorithm.DDA_RANDOM_THETA || (Algorithm == DDAAlgorithm.DDA_RANDOM_LOGREG && !diffParams.LogRegReady))
        {
            diffParams.Theta = Random.Range(0.0f, 1.0f);
            diffParams.AlgorithmActuallyUsed = DDAAlgorithm.DDA_RANDOM_THETA;

            //If regression is okay, we can tell the difficulty for this theta
            if (diffParams.LogRegReady)
            {
                double[] pars = new double[1];
                pars[0] = diffParams.Theta;
                diffParams.TargetDiff = 1.0 - LogReg.Predict(pars);
                diffParams.TargetDiffWithExplo = diffParams.TargetDiff;
            }
            else //Otherwise, we don't know, let's put a negative value
            {
                diffParams.TargetDiffWithExplo = -1;
                diffParams.TargetDiff = -1;
            }
        }

        //Save betas if we have some
        if (LogReg != null && LogReg.Betas != null && LogReg.Betas.Length > 0)
        {
            diffParams.Betas = new double[LogReg.Betas.Length];
            for (int i = 0; i < LogReg.Betas.Length; i++)
                diffParams.Betas[i] = LogReg.Betas[i];
        }

        //Clamp 01 double. Super inportant pour éviter les infinis
        diffParams.Theta = diffParams.Theta > 1.0 ? 1.0 : diffParams.Theta;
        diffParams.Theta = diffParams.Theta < 0.0 ? 0.0 : diffParams.Theta;

        return diffParams;
    }

    public bool checkDataAgainst(List<DDADataManager.Attempt> attempts)
    {
        List<DDADataManager.Attempt> attemptsSaved = DataManager.getAttempts(PlayerId, ChallengeId, LRNbLastAttemptsToConsider);

        bool isSame = true;
        int nbCheck = 0;
        for (int i = 0; i < attempts.Count; i++)
        {
            if (i >= attempts.Count - LRNbLastAttemptsToConsider)
            {
                if (!attempts[i].IsSame(attemptsSaved[i - (attempts.Count - LRNbLastAttemptsToConsider)]))
                {
                    Debug.LogError("Attempt " + i + " is corrupted");
                    isSame = false;
                }
                nbCheck++;
            }

        }

        if (isSame)
            Debug.Log("Data is ok, checked " + nbCheck + " attempts (cache size)");

        return isSame;

    }
}