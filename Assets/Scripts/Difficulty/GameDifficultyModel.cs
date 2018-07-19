using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * Pour utiliser le modèle de difficulté :
 * 1) appeler setProfile avec le nom du joueur et l'activité en cours. 
 *    les datas sont chargées et le modèle se met à jour automatiquement
 * 2) appeler predictDifficulty pour prédire la difficulté d'un essai
 * 3) appeler addTry pour ajouter un nouvel du joueur au modèle et le sauver 
 */

public class GameDifficultyModel : MonoBehaviour
{
    public int NbLastSamples = 500;

#if UNITY_EDITOR
    bool calcAccuracy = true;
#else
    bool calcAccuracy = false;
#endif

    LogisticRegression.ModelLR Model;
    double Accuracy = 0;
    bool AccuracyUpToDate = false;
    List<double[]> Tries;
    List<double> Results;
    string FileDataName = "data.csv";
    string FileModelName = "model.csv";    
    bool ProfileSet = false;
    
    /**
     * Permet de remonter dans l'historique du joueur, au moins pour le dernier try
     * Retourne un tableau avec les valeurs des variables puis le résultat
     */
    public double [] getLastTryAndRes()
    {
        if (Tries.Count == 0)
            return null;

        double[] lastTry = Tries[Tries.Count - 1];
        double[] lastTryAndRes = new double[lastTry.Length + 1];
        for(int i=0;i< lastTry.Length; i++)
        {
            lastTryAndRes[i] = lastTry[i];
        }
        lastTryAndRes[lastTry.Length] = Results[Results.Count - 1];

        return lastTryAndRes;
    }

    /**
     * Essaie d'estimer la qualité du modèle.
     * Si on a moins de 10 points, on retourne 0
     * Si on a plus de 10 points, on calcule la qualité de prédiction avec une crossval tenfold
     */
    public double getModelQuality() {
        if (Results == null)
            return 0;
        if (Results.Count < 10)
        {
            Debug.Log("Moins de 10 résultats, model inutilisable");
            return 0;
        }
            
        //on compte les 1 et les 0
        double nb0 = 0;
        double nb1 = 0;
        for (int i=0;i< Results.Count; i++)
        {
            if (Results[i] == 0)
                nb0++;
            else
                nb1++;
        }

        //Si on a pas assez de 1 ou pas assez de 0
        if (nb1 <= 3 || nb0 <= 3)
        {
            Debug.Log("Moins de 3 wins ou 3 fails, model inutilisable");
            return 0;
        }

        return getAccuracy();
    }

    double getAccuracy()
    {
        if (!AccuracyUpToDate)
            updateModel(true);

        return Accuracy;
    }

    /**
     * Choisit et charge le bon fichier de data pour le joueur et l'activité
     * Attention les id sont convertis en nom de fichiers, ne pas utiliser
     * de caractères spéciaux ou de noms trop longs
     */
    public void setProfile(string userId, string activityId)
    {
        FileDataName = userId + "_" + activityId + ".csv";
        FileModelName = userId + "_" + activityId + "_model.csv";
        ProfileSet = true;
        loadFromDisk();
        updateModel(true);
    }

    /**
     * Ajoute un essai au modèle et le sauve
     * diffVars : valeur des paramètres du challenge que le joueur vient de tenter de réussir
     *            utiliser toujours le même ordre pour les différents paramètres d'un meme challenge
     * win : si le joueur a réussi ou pas son essai
     */
    public void addTry(double[] diffVars, bool win, bool save=true, bool update=true)
    {
        if (!ProfileSet)
            throw new Exception("addTry but no profile set");

        if(Tries == null)
        {
            Tries = new List<double[]>();
            Results = new List<double>();
        }

        if(diffVars == null)
        {
            Debug.Log("ERROR: adding try with no diff vars. Not Adding");
            return;
        }

        Tries.Add(diffVars);
        Results.Add(win ? 1 : 0);
        AccuracyUpToDate = false;

        if (save)
            saveToDisk();

        if (update)
            updateModel();
    }

    /**
     * Prédit la probabilité d'échec en fonction des variables de difficulté
     * vars : valeur des paramètres du challenge dont on souhaite prédire la difficulté
     */
    public double predictDifficulty(double[] vars)
    {
        if (!ProfileSet)
            throw new Exception("predictDifficulty but no profile set");

        return 1.0-Model.Predict(vars);
    }

    /**
     * Donne la valeur d'un paramètre, étant donné la valeur des autres paramètres et la proba de fail
     * probaFail : probabilité de fail voulue
     * vars : valeur des paramètres de difficulté (sauf celui a prédire qui ne sera pas lu) : si plus de une variable
     * varToSet : indice de la variable qu'on souhaite prédire : si plus de une variable (on peut pas résoudre pour n variables)
     */
    public double getDiffParameter(double probaFail, double[] vars = null, int varToSet = 0)
    {
        if (!ProfileSet)
            throw new Exception("predictDifficulty but no profile set");

        probaFail = System.Math.Min(1.0, System.Math.Max(0, probaFail));
        return Model.InvPredict(1.0-probaFail, vars, varToSet);
    }

    /**
     *  NORMALEMENT LES FONCTION SUIVANTES SONT APPELEES DE MANIERE AUTO
     */

    public void Awake()
    {
        UnitySystemConsoleRedirector.Redirect();
        Reset();
    }

    public void Reset()
    {
        Tries = new List<double[]>();
        Results = new List<double>();
    }

    public void updateModel(bool updateAccuracy = true)
    {
        if (!ProfileSet)
            throw new Exception("updateModel but no profile set");

        //Loading data
        LogisticRegression.DataLR data = new LogisticRegression.DataLR();
        data.LoadDataFromList(Tries, Results);

        //On ne garde que les n derniers car apprentissage
        data = data.getLastNRows(NbLastSamples);
       

        data.saveDataToCsv(Application.persistentDataPath + "/usedToTrain.csv");

        Debug.Log("Using " + data.DepVar.Length + " lines to update model");

        if (updateAccuracy)
        {
            //Ten fold cross val
            Accuracy = 0;
            
            for (int i = 0; i < 10; i++)
            {
                double AccuracyNow = 0;
                data = data.shuffle();
                int nk = 10;
                for (int k = 0; k < nk; k++)
                {
                    LogisticRegression.DataLR dataTrain;
                    LogisticRegression.DataLR dataTest;
                    data.split(k * (100/nk), (k + 1) * (100 / nk), out dataTrain, out dataTest);
                    Model = LogisticRegression.ComputeModel(dataTrain);
                    AccuracyNow += LogisticRegression.TestModel(Model, dataTest);
                }
                AccuracyNow /= nk;
                Accuracy += AccuracyNow;
            }
            Accuracy /= 10;

            AccuracyUpToDate = true;
            Debug.Log("Updating accuracy : " + Accuracy);
        }

        //Using all data to update model
        Model = LogisticRegression.ComputeModel(data);

        if (!Model.isUsable())
            return;

        //On vérifie si on a pas des soucis de modèle complètement dans les choux
        //Prédiction de difficulté doit marcher dans les deux sens
        double errorSum = 0;
        double diffTest = 0.1;
        double [] pars = new double[1];
        double[] parsForAllDiff = new double[10];
        string res = "";
        for (int i = 0; i < 8; i++)
        {
            pars[0] = Model.InvPredict(diffTest,pars,0); //on regarde que la première variable.
            parsForAllDiff[i] = pars[0];
            res = "Diff = " + diffTest + " par = " + pars[0];
            errorSum += System.Math.Abs(diffTest - Model.Predict(pars)); //On passe dans les deux sens on doit avoir pareil
            res += " res = " + Model.Predict(pars) + "\n";
            diffTest += 0.1;
            Debug.Log(res);
        }

        if (errorSum > 1)
        {
            Debug.Log("Model is not solid, error = " + errorSum);
            Accuracy = 0;
        }

        //On calcule l'écart type des paramètres calculés pour toute la gamme de difficulté 0 - 1
        //En gros si on a toujours le meme paramètre, on a un souci
        double mean = 0;
        for (int i = 0; i < 8; i++)
            mean += parsForAllDiff[i];
        mean /= 8;
        double sd = 0;
        for (int i = 0; i < 8; i++)
            sd += (parsForAllDiff[i] - mean)* (parsForAllDiff[i] - mean);
        sd = System.Math.Sqrt(sd);

        Debug.Log("Model parameter estimation sd = " + sd);

        if (sd < 0.05 || double.IsNaN(sd))
        {
            Debug.Log("Model parameter estimation is always the same : sd=" + sd);
            Accuracy = 0;
        }      
            
    }

    

    public void saveToDisk()
    {
        if (!ProfileSet)
            throw new Exception("saveToDisk but no profile set");

        Debug.Log("Saving to disk");
        LogisticRegression.DataLR data = new LogisticRegression.DataLR();
        data.LoadDataFromList(Tries, Results);
        data.saveDataToCsv(Application.persistentDataPath + "/" + FileDataName);
        Debug.Log(data.DepVar.Length + " tries saved to " + Application.persistentDataPath + "/" + FileDataName);

        //On sauve aussi l'état du modèle pour nous
        Model.saveBetasToCsv(Application.persistentDataPath + "/" + FileModelName);
        Debug.Log("Betas saved to " + Application.persistentDataPath + "/" + FileDataName);
    }

    void loadFromDisk()
    {
        if (!ProfileSet)
            throw new Exception("loadFromDisk but no profile set");

        Tries = new List<double[]>();
        Results = new List<double>();

        Debug.Log("Loading from disk");

        LogisticRegression.DataLR data = new LogisticRegression.DataLR();
        try
        {
            data.LoadDataFromCsv(Application.persistentDataPath + "/" + FileDataName);
        }catch(FormatException e)
        {
            Debug.Log("Player data corrupted !!!! Erasing ("+e.Message+")");
            Reset();
            saveToDisk();
        }
        
        if (data.IndepVar == null)
            return;

        Debug.Log("Adding " + data.DepVar.Length + " tries to the GameDifficulty lists");

        int row = 0;
        
        foreach (double[] vars in data.IndepVar)
        {
            double[] varsWithoutIntercept = new double[data.IndepVar[0].Length - 1];
            for (int i = 0; i < vars.Length - 1; i++)
                varsWithoutIntercept[i] = vars[i + 1];
            addTry(varsWithoutIntercept, data.DepVar[row] > 0,false,false);
            row++;
        }
    }

    public double [] getBetas()
    {
        if(Model.Betas != null)
            return Model.Betas.Clone() as double[];
        return null;
    }


}
