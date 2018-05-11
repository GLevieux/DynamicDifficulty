using System.Collections.Generic;
using System;
using System.IO;
using System.IO.IsolatedStorage;

class LogisticRegression
{
    public class DataLR
    {
        public double[][] IndepVar; //Valeurs des variables dont on compute les betas, plus une colonne de 1 au debut
        public double[] DepVar; //Valeurs binaires qu'on cherche à prédire

        public DataLR()
        {
            IndepVar = MatrixCreate(0, 0);
            DepVar = new double[0];
        }

        //shuffle
        public DataLR shuffle()
        {
            DataLR part = new DataLR();

            if (DepVar.Length == 0)
                return part;

            int nbRows = DepVar.Length;
            int nbVars = IndepVar[0].Length;

            part.IndepVar = new double[nbRows][];
            part.DepVar = VectorCreate(nbRows);

            Random r = new Random();
            int row = 0;
            foreach (double[] vars in IndepVar)
            {
                //On tire une ligne a remplir au hasard
                int rowRand = r.Next() % nbRows;
                //sens dans lequel on cherche une case vide
                int sens = r.NextDouble() > 0.5 ? -1 : 1;

                //On cherche une case vide
                int nextRow = -1;
                if(sens > 0)
                {
                    for(int i=0;i<nbRows;i++)
                    {
                        int rowTest = (rowRand + i) % nbRows;
                        if (part.IndepVar[rowTest] == null)
                            nextRow = rowTest;
                    }
                }
                else
                {
                    for (int i = 0; i < nbRows; i++)
                    {
                        int rowTest = (rowRand - i);
                        if (rowTest < 0)
                            rowTest += nbRows;
                        if (part.IndepVar[rowTest] == null)
                            nextRow = rowTest;
                    }
                }

                if (nextRow < 0)
                    throw new Exception("Pas trouvé de case vide, algo de shuffle marche pas");

                part.IndepVar[nextRow] = new double[nbVars];
                
                for (int i = 0; i < vars.Length; i++)
                {
                    part.IndepVar[nextRow][i] = vars[i];
                }
                part.DepVar[nextRow] = DepVar[row];

                ++row;
            }

            return part;
        }

        //split in two sub samples 
        public void split(int pcentStartExtract, int pcentEndExtract, out DataLR partOut, out DataLR partIn)
        {
            partOut = new DataLR();
            partIn = new DataLR();

            if (DepVar.Length == 0)
                return;

            int nbLignes = DepVar.Length;
            int nbVars = IndepVar[0].Length;

            int iStart = (nbLignes * pcentStartExtract) / 100;
            int iEnd = (nbLignes * pcentEndExtract) / 100;
            int nbRowsIn = iEnd - iStart;
            int nbRowsOut = DepVar.Length - nbRowsIn;

            partIn.IndepVar = MatrixCreate(nbRowsIn, nbVars);
            partIn.DepVar = new double[nbRowsIn];

            partOut.IndepVar = MatrixCreate(nbRowsOut, nbVars);
            partOut.DepVar = new double[nbRowsOut];

            int row = 0;
            int rowIn = 0;
            int rowOut = 0;
            foreach (double[] vars in IndepVar)
            {
                //out of section
                if (row < iStart || row >= iEnd)
                {
                    for (int i = 0; i < vars.Length; i++)
                        partOut.IndepVar[rowOut][i] = vars[i];
                    partOut.DepVar[rowOut] = DepVar[row];
                    ++rowOut;
                }

                //in section
                if (row >= iStart && row < iEnd)
                {
                    for (int i = 0; i < vars.Length; i++)
                        partIn.IndepVar[rowIn][i] = vars[i];
                    partIn.DepVar[rowIn] = DepVar[row];
                    ++rowIn;
                }

                ++row;
            }

            //Console.WriteLine("Partion split: partIn=" + rowIn + " partOut:" + rowOut);
        }

        public DataLR getLastNRows(int nbRows)
        {
            DataLR part = new DataLR();
            if(DepVar.Length > 0)
            {
                int nbRowsTake = Math.Min(nbRows, DepVar.Length);
                int nbVars = IndepVar[0].Length;
                int iStart = DepVar.Length - nbRowsTake;

                part.IndepVar = MatrixCreate(nbRowsTake, nbVars);
                part.DepVar = VectorCreate(nbRowsTake);

                int row = 0;
                int rowLoad = 0;
                foreach (double[] vars in IndepVar)
                {
                    //out of section
                    if (row >= iStart)
                    {
                        for (int i = 0; i < vars.Length; i++)
                            part.IndepVar[rowLoad][i] = vars[i];
                        part.DepVar[rowLoad] = DepVar[row];
                        ++rowLoad;
                    }
                    ++row;
                }
            }
       
            return part;
        }
        

        public void LoadDataFromList(List<double[]> indepVars, List<double> depVars)
        {
            if(indepVars == null || indepVars.Count == 0)
            {
                IndepVar = MatrixCreate(0, 0);
                DepVar = new double[0];
                return;
            }

            int nbRows = depVars.Count;
            IndepVar = MatrixCreate(nbRows, indepVars[0].Length + 1);
            DepVar = new double[nbRows];

            int row = 0;
            foreach (double[] vars in indepVars)
            {
                IndepVar[row][0] = 1;
                for (int i = 0; i < vars.Length; i++)
                    IndepVar[row][i+1] = vars[i];
                DepVar[row] = depVars[row];
                ++row;
            }
        }


        public void LoadDataFromCsv(string csvFile)
        {
            //On compte le nombre de lignes et de variables
            try
            {
                FileStream ifs = new FileStream(csvFile, FileMode.Open);
                StreamReader sr = new StreamReader(ifs);
                string line = "";
                string[] tokens = null;
                int ct = 0;
                int nbVars = -1;
                bool bHeaders = false;
                while ((line = sr.ReadLine()) != null) // count number lines in file
                {
                    //Si lgne 1, on test si headers
                    if (ct == 0)
                    {
                        line = line.Trim();
                        tokens = line.Split(';');
                        double result;
                        bHeaders = !double.TryParse(tokens[0], out result);
                    }
                    if (nbVars < 0)
                    {
                        line = line.Trim();
                        tokens = line.Split(';');
                        nbVars = tokens.Length;
                        nbVars--; //On compté aussi la variable dépendante
                    }
                    ++ct;
                }

                if (bHeaders)
                    ct--;

                sr.Close(); ifs.Close();


                //On parse le fichier pour charger les datas
                IndepVar = MatrixCreate(ct, nbVars + 1);
                DepVar = VectorCreate(ct);
                ifs = new FileStream(csvFile, FileMode.Open);
                sr = new StreamReader(ifs);
                int row = 0;

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    tokens = line.Split(';');
                    IndepVar[row][0] = 1.0;
                    for (int i = 0; i < nbVars; i++)
                        IndepVar[row][i + 1] = double.Parse(tokens[i]);
                    DepVar[row] = double.Parse(tokens[tokens.Length - 1]);
                    ++row;
                }
                sr.Close(); ifs.Close();
            }
            catch (FileNotFoundException e)
            {
                IndepVar = MatrixCreate(0, 0);
                DepVar = new double[0];
                Console.WriteLine("File "+ csvFile + " not found ("+e.Message+")");
            }
            catch(IsolatedStorageException e)
            {
                IndepVar = MatrixCreate(0, 0);
                DepVar = new double[0];
                Console.WriteLine("File " + csvFile + " not found (" + e.Message + ")");
            }
        }

        public void saveDataToCsv(string csvFile)
        {
            try
            {
                FileStream ofs = new FileStream(csvFile, FileMode.Create);
                
                StreamWriter sw = new StreamWriter(ofs);

                int row = 0;
                foreach (double[] vars in IndepVar)
                {
                    for (int i = 1; i < vars.Length; i++)
                    {
                        sw.Write(vars[i]);
                        sw.Write(";");
                    }
                    sw.Write(DepVar[row]);
                    sw.Write("\n");
                    ++row;
                }

                sw.Flush();
                ofs.Flush();
                sw.Close();
                ofs.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public class ModelLR
    {
        public double[] Betas;

        public ModelLR()
        {
            Betas = new double[0];
        }

        public double Predict(double[] values)
        {
            // p = 1 / (1 + exp(-z) where z = b0x0 + b1x1 + b2x2 + b3x3 + . . .

            if (values.Length != Betas.Length-1)
                throw new Exception("Impossible to predict, not good number of variables");

            double result = 0; // ex: if xMatrix is size 10 x 4 and bVector is 4 x 1 then prob vector is 10 x 1 (one prob for every row of xMatrix)

            double z = 0.0;

            z = 1.0 * Betas[0]; // b0(1.0)
            for (int i = 0; i < Betas.Length-1; ++i)
            {
                z += values[i] * Betas[i+1]; // z + b1x1 + b2x2 + . . .
            }
            result = 1.0 / (1.0 + Math.Exp(-z));  // consider checking for huge value of Math.Exp(-z) here

            return result;
        }


        

        //trouve le bon params xi pour une proba donnée et toutes les variables xj(j!=i) fixées sauf une (sinon pas de res)
        //xi = ( (-ln(1/p -1) - (b(j!=i)x(j!=i)) ) / bi;
        public double InvPredict(double proba, double [] values = null, int varToSet = 0)
        {
            double valueXi = 0;

            if (Betas.Length == 0)
                return 0.0f;

            double sommeBjXjNotI = 1.0 * Betas[0]; // b0(1.0)

            //Si une seule vairable, on fait direct la prédiction , pas besoin de bloquer les autres
            if(Betas.Length == 2)
            {
                valueXi = ((-Math.Log(1.0 / proba) - sommeBjXjNotI)) / Betas[1];
            }
            else
            {
                for (int i = 0; i < Betas.Length-1; ++i)
                {
                    if (i != varToSet)
                        sommeBjXjNotI += values[i] * Betas[i + 1]; // z + b1x1 + b2x2 + . . .
                }
                valueXi = ((-Math.Log(1.0 / proba - 1) - sommeBjXjNotI)) / Betas[varToSet + 1];
            }
                       
            return valueXi;
        }

    }

    //Le fichier doit contenir pour chaque lignes les valeurs des indépendants suivie de la dépendante
    public static ModelLR ComputeModel(DataLR datas)
    {
        ModelLR model = new ModelLR();

        try
        {
            int maxIterations = 25;
            double epsilon = 0.01; // stop if all new beta values change less than epsilon (algorithm has converged?)
            double jumpFactor = 1000.0; // stop if any new beta jumps too much (algorithm spinning out of control?)

            model.Betas = ComputeBestBeta(datas.IndepVar, datas.DepVar, maxIterations, epsilon, jumpFactor); // computing the beta parameters is synonymous with 'training'
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fatal in ComputeBestBeta: " + ex.Message);
        }

        return model;
    }
    
    public static double TestModel(ModelLR model, DataLR testData)
    {
        double acc = 0;
        try
        {
            acc = (double)PredictiveAccuracy(testData.IndepVar, testData.DepVar, model.Betas)/100.0; // percent of data cases correctly predicted in the test data set.
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fatal in TestModel: " + ex.Message);
        }

        return acc;
    }
    
    static double PredictiveAccuracy(double[][] xMatrix, double[] yVector, double[] bVector)
    {
        // returns the percent (as 0.00 to 100.00) accuracy of the bVector measured by how many lines of data are correctly predicted.
        // note: this is not the same as accuracy as measured by sum of squared deviations between 
        // the probabilities produceed by bVector and 0.0 and 1.0 data in yVector
        // For predictions we simply see if the p produced by b are >= 0.50 or not.

        if (xMatrix == null || xMatrix.Length == 0 || yVector == null || bVector == null)
            return 0;

        int xRows = xMatrix.Length; int xCols = xMatrix[0].Length;
        int yRows = yVector.Length;
        int bRows = bVector.Length;
        if (xCols != bRows || xRows != yRows)
            throw new Exception("Bad dimensions for xMatrix or yVector or bVector in PredictiveAccuracy()");

        int numberCasesCorrect = 0;
        int numberCasesWrong = 0;
        double[] pVector = ConstructProbVector(xMatrix, bVector); // helper also used by LogisticRegressionNewtonParameters()
        int pRows = pVector.Length;
        if (pRows != xRows)
            throw new Exception("Unequal rows in prob vector and design matrix in PredictiveAccuracy()");

        for (int i = 0; i < yRows; ++i) // each dependent variable
        {
            if (pVector[i] >= 0.50 && yVector[i] == 1.0)
                ++numberCasesCorrect;
            else if (pVector[i] < 0.50 && yVector[i] == 0.0)
                ++numberCasesCorrect;
            else
                ++numberCasesWrong;
        }

        int total = numberCasesCorrect + numberCasesWrong;
        if (total == 0)
            return 0.0;
        else
            return (100.0 * numberCasesCorrect) / total;
    } // PredictiveAccuracy

    // ============================================================================================

    static double[] ComputeBestBeta(double[][] xMatrix, double[] yVector, int maxIterations, double epsilon, double jumpFactor)
    {
        // Use the Newton-Raphson technique to estimate logistic regression beta parameters
        // xMatrix is a design matrix of predictor variables where the first column is augmented with all 1.0 to represent dummy x values for the b0 constant
        // yVector is a column vector of binary (0.0 or 1.0) dependent variables
        // maxIterations is the maximum number of times to iterate in the algorithm. A value of 1000 is reasonable.
        // epsilon is a closeness parameter: if all new b[i] values after an iteration are within epsilon of
        // the old b[i] values, we assume the algorithm has converged and we return. A value like 0.001 is often reasonable.
        // jumpFactor stops the algorithm if any new beta value is jumpFactor times greater than the old value. A value of 1000.0 seems reasonable.
        // The return is a column vector of the beta estimates: b[0] is the constant, b[1] for x1, etc.
        // There is a lot that can go wrong here. The algorithm involves finding a matrx inverse (see MatrixInverse) which will throw
        // if the inverse cannot be computed. The Newton-Raphson algorithm can generate beta values that tend towards infinity. 
        // If anything bad happens the return is the best beta values known at the time (which could be all 0.0 values but not null).

        if (xMatrix.Length == 0)
            return null;

        int xRows = xMatrix.Length;
        int xCols = xMatrix[0].Length;

        if (xRows != yVector.Length)
            throw new Exception("The xMatrix and yVector are not compatible in LogisticRegressionNewtonParameters()");

        // initial beta values
        double[] bVector = new double[xCols];
        for (int i = 0; i < xCols; ++i) { bVector[i] = 0.0; } // initialize to 0.0. TODO: consider alternatives
                                                              //Console.WriteLine("The initial B vector is");
                                                              //Console.WriteLine(VectorAsString(bVector)); Console.WriteLine("\n");

        // best beta values found so far
        double[] bestBvector = VectorDuplicate(bVector);

        double[] pVector = ConstructProbVector(xMatrix, bVector); // a column vector of the probabilities of each row using the b[i] values and the x[i] values.
                                                                  //Console.WriteLine("The initial Prob vector is: ");
                                                                  //Console.WriteLine(VectorAsString(pVector)); Console.WriteLine("\n");

        //double[][] wMatrix = ConstructWeightMatrix(pVector); // deprecated. not needed if we use a shortct to comput WX. See ComputeXtilde.
        //Console.WriteLine("The initial Weight matrix is: ");
        //Console.WriteLine(MatrixAsString(wMatrix)); Console.WriteLine("\n");

        double mse = MeanSquaredError(pVector, yVector);
        int timesWorse = 0; // how many times are the new betas worse (i.e., give worse MSE) than the current betas

        for (int i = 0; i < maxIterations; ++i)
        {
            //Console.WriteLine("=================================");
            //Console.WriteLine(i);

            double[] newBvector = ConstructNewBetaVector(bVector, xMatrix, yVector, pVector); // generate new beta values using Newton-Raphson. could return null.
            if (newBvector == null)
            {
                //Console.WriteLine("The ConstructNewBetaVector() helper method in LogisticRegressionNewtonParameters() returned null");
                //Console.WriteLine("because the MatrixInverse() helper method in ConstructNewBetaVector returned null");
                //Console.WriteLine("because the current (X'X~) product could not be inverted");
                //Console.WriteLine("Returning best beta vector found");
                //Console.ReadLine();
                return bestBvector;
            }

            //Console.WriteLine("New b vector is ");
            //Console.WriteLine(VectorAsString(newBvector)); Console.WriteLine("\n");

            // no significant change?
            if (NoChange(bVector, newBvector, epsilon) == true) // we are done because of no significant change in beta[]
            {
                //Console.WriteLine("No significant change between old beta values and new beta values -- stopping");
                //Console.ReadLine();
                return bestBvector;
            }
            // spinning out of control?
            if (OutOfControl(bVector, newBvector, jumpFactor) == true) // any new beta more than jumpFactor times greater than old?
            {
                //Console.WriteLine("The new beta vector has at least one value which changed by a factor of " + jumpFactor + " -- stopping");
                //Console.ReadLine();
                return bestBvector;
            }

            pVector = ConstructProbVector(xMatrix, newBvector);

            // are we getting worse or better?
            double newMSE = MeanSquaredError(pVector, yVector); // smaller is better
            if (newMSE > mse) // new MSE is worse than current SSD
            {
                ++timesWorse;           // update counter
                if (timesWorse >= 4)
                {
                    //Console.WriteLine("The new beta vector produced worse predictions even after modification four times in a row -- stopping");
                    return bestBvector;
                }
                //Console.WriteLine("The new beta vector has produced probabilities which give worse predictions -- modifying new betas to halfway between old and new");
                //Console.WriteLine("Times worse = " + timesWorse);

                bVector = VectorDuplicate(newBvector);   // update current b: old b becomes not the new b but halfway between new and old
                for (int k = 0; k < bVector.Length; ++k) { bVector[k] = (bVector[k] + newBvector[k]) / 2.0; }
                mse = newMSE;                            // update current SSD (do not update best b because we don't have a new best b)
                                                         //Console.ReadLine();
            }
            else // new SSD is be better than old
            {
                bVector = VectorDuplicate(newBvector);  // update current b: old b becomes new b
                bestBvector = VectorDuplicate(bVector); // update best b
                mse = newMSE;                           // update current MSE
                timesWorse = 0;                         // reset counter
            }

            //double pa = PredictiveAccuracy(xMatrix, yVector, bestBvector); // how many cases are we correctly predicting
            //Console.WriteLine("Predictive accuracy is " + pa.ToString("F4"));

            //Console.WriteLine("=================================");
            //Console.ReadLine();
        } // end main iteration loop

        //Console.WriteLine("Exceeded max iterations -- stopping");
        //Console.ReadLine();
        return bestBvector;

    } // ComputeBestBeta

    // --------------------------------------------------------------------------------------------

    static double[] ConstructNewBetaVector(double[] oldBetaVector, double[][] xMatrix, double[] yVector, double[] oldProbVector)
    {
        // this is the heart of the Newton-Raphson technique
        // b[t] = b[t-1] + inv(X'W[t-1]X)X'(y - p[t-1])
        //
        // b[t] is the new (time t) b column vector
        // b[t-1] is the old (time t-1) vector
        // X' is the transpose of the X matrix of x data (1.0, age, sex, chol)
        // W[t-1] is the old weight matrix
        // y is the column vector of binary dependent variable data
        // p[t-1] is the old column probability vector (computed as 1.0 / (1.0 + exp(-z) where z = b0x0 + b1x1 + . . .)

        // note: W[t-1] is nxn which could be huge so instead of computing b[t] = b[t-1] + inv(X'W[t-1]X)X'(y - p[t-1])
        // compute b[t] = b[t-1] + inv(X'X~)X'(y - p[t-1]) where X~ is W[t-1]X computed directly
        // the idea is that the vast majority of W[t-1] cells are 0.0 and so can be ignored

        double[][] Xt = MatrixTranspose(xMatrix);                 // X'
        double[][] A = ComputeXtilde(oldProbVector, xMatrix);     // WX
        double[][] B = MatrixProduct(Xt, A);                      // X'WX

        double[][] C = MatrixInverse(B);                          // inv(X'WX)
        if (C == null)                                            // computing the inverse can blow up easily
            return null;

        double[][] D = MatrixProduct(C, Xt);                      // inv(X'WX)X'
        double[] YP = VectorSubtraction(yVector, oldProbVector);  // y-p
        double[] E = MatrixVectorProduct(D, YP);                  // inv(X'WX)X'(y-p)
        double[] result = VectorAddition(oldBetaVector, E);       // b + inv(X'WX)X'(y-p)

        return result;                                            // could be null!
    } // ConstructNewBvector

    // --------------------------------------------------------------------------------------------

    static double[][] ComputeXtilde(double[] pVector, double[][] xMatrix)
    {
        // note: W[t-1] is nxn which could be huge so instead of computing b[t] = b[t-1] + inv(X'W[t-1]X)X'(y - p[t-1]) directly
        // we compute the W[t-1]X part, without the use of W.
        // Since W is derived from the prob vector and W has non-0.0 elements only on the diagonal we can avoid a ton of work
        // by using the prob vector directly and not computing W at all.
        // Some of the research papers refer to the product W[t-1]X as X~ hence the name of this method.
        // ex: if xMatrix is 10x4 then W would be 10x10 so WX would be 10x4 -- the same size as X

        int pRows = pVector.Length;
        int xRows = xMatrix.Length;
        int xCols = xMatrix[0].Length;

        if (pRows != xRows)
            throw new Exception("The pVector and xMatrix are not compatible in ComputeXtilde");

        // we are not doing marix multiplication. the p column vector sort of lays on top of each matrix column.
        double[][] result = MatrixCreate(pRows, xCols); // could use (xRows, xCols) here

        for (int i = 0; i < pRows; ++i)
        {
            for (int j = 0; j < xCols; ++j)
            {
                result[i][j] = pVector[i] * (1.0 - pVector[i]) * xMatrix[i][j]; // note the p(1-p)
            }
        } // i
        return result;
    } // ComputeXtilde


    // --------------------------------------------------------------------------------------------

    static bool NoChange(double[] oldBvector, double[] newBvector, double epsilon)
    {
        // true if all new b values have changed by amount smaller than epsilon
        for (int i = 0; i < oldBvector.Length; ++i)
        {
            if (Math.Abs(oldBvector[i] - newBvector[i]) > epsilon) // we have at least one change
                return false;
        }
        return true;
    } // NoChange

    static bool OutOfControl(double[] oldBvector, double[] newBvector, double jumpFactor)
    {
        // true if any new b is jumpFactor times greater than old b
        for (int i = 0; i < oldBvector.Length; ++i)
        {
            if (oldBvector[i] == 0.0) return false; // if old is 0.0 anything goes for the new value

            if (Math.Abs(oldBvector[i] - newBvector[i]) / Math.Abs(oldBvector[i]) > jumpFactor) // too big a change.
                return true;
        }
        return false;
    }

    // --------------------------------------------------------------------------------------------

    static double[] ConstructProbVector(double[][] xMatrix, double[] bVector)
    {
        // p = 1 / (1 + exp(-z) where z = b0x0 + b1x1 + b2x2 + b3x3 + . . .
        // suppose X is 10 x 4 (cols are: x0 = const. 1.0, x1, x2, x3)
        // then b would be a 4 x 1 (col vecror)
        // then result of X times b is (10x4)(4x1) = (10x1) column vector

        int xRows = xMatrix.Length;
        int xCols = xMatrix[0].Length;
        int bRows = bVector.Length;

        if (xCols != bRows)
            throw new Exception("xMatrix and bVector are not compatible in ConstructProbVector");

        double[] result = VectorCreate(xRows); // ex: if xMatrix is size 10 x 4 and bVector is 4 x 1 then prob vector is 10 x 1 (one prob for every row of xMatrix)

        double z = 0.0;
        double p = 0.0;

        for (int i = 0; i < xRows; ++i)
        {
            z = 0.0;
            for (int j = 0; j < xCols; ++j)
            {
                z += xMatrix[i][j] * bVector[j]; // b0(1.0) + b1x1 + b2x2 + . . .
            }
            p = 1.0 / (1.0 + Math.Exp(-z));  // consider checking for huge value of Math.Exp(-z) here
            result[i] = p;
        }
        return result;
    } // ConstructProbVector

    // --------------------------------------------------------------------------------------------

    static double MeanSquaredError(double[] pVector, double[] yVector)
    {
        // how good are the predictions? (using an already-calculated prob vector)
        // note: it is possible that a model with better (lower) MSE than a second model could give worse predictive accuracy.
        int pRows = pVector.Length;
        int yRows = yVector.Length;
        if (pRows != yRows)
            throw new Exception("The prob vector and the y vector are not compatible in MeanSquaredError()");
        if (pRows == 0)
            return 0.0;
        double result = 0.0;
        for (int i = 0; i < pRows; ++i)
        {
            result += (pVector[i] - yVector[i]) * (pVector[i] - yVector[i]);
            //result += Math.Abs(pVector[i] - yVector[i]); // average absolute deviation approach
        }
        return result / pRows;
    }

    // --------------------------------------------------------------------------------------------

    // ============================================================================================

    static double[][] MatrixCreate(int rows, int cols)
    {
        // creates a matrix initialized to all 0.0. assume rows and cols > 0
        double[][] result = new double[rows][];
        for (int i = 0; i < rows; ++i) { result[i] = new double[cols]; } // explicit initialization not necessary.
        return result;
    }

    static double[] VectorCreate(int rows) // all vectors in Newton-Raphson are single-column vectors
    {
        double[] result = new double[rows]; // we use this technique when we want to make column vector creation explicit
        return result;
    }

    static string MatrixAsString(double[][] matrix, int numRows, int digits, int width)
    {
        string s = "";
        for (int i = 0; i < matrix.Length && i < numRows; ++i)
        {
            for (int j = 0; j < matrix[i].Length; ++j)
            {
                s += matrix[i][j].ToString("F" + digits).PadLeft(width) + " ";
            }
            s += Environment.NewLine;
        }
        return s;
    } // MatrixAsString

    static double[][] MatrixDuplicate(double[][] matrix)
    {
        // allocates/creates a duplicate of a matrix. assumes matrix is not null.
        double[][] result = MatrixCreate(matrix.Length, matrix[0].Length);
        for (int i = 0; i < matrix.Length; ++i) // copy the values
            for (int j = 0; j < matrix[i].Length; ++j)
                result[i][j] = matrix[i][j];
        return result;
    }

    static double[] VectorAddition(double[] vectorA, double[] vectorB)
    {
        int aRows = vectorA.Length;
        int bRows = vectorB.Length;
        if (aRows != bRows)
            throw new Exception("Non-conformable vectors in VectorAddition");
        double[] result = new double[aRows];
        for (int i = 0; i < aRows; ++i)
            result[i] = vectorA[i] + vectorB[i];
        return result;
    }

    static double[] VectorSubtraction(double[] vectorA, double[] vectorB)
    {
        int aRows = vectorA.Length;
        int bRows = vectorB.Length;
        if (aRows != bRows)
            throw new Exception("Non-conformable vectors in VectorSubtraction");
        double[] result = new double[aRows];
        for (int i = 0; i < aRows; ++i)
            result[i] = vectorA[i] - vectorB[i];
        return result;
    }

    static string VectorAsString(double[] vector, int count, int digits, int width)
    {
        string s = "";
        for (int i = 0; i < vector.Length && i < count; ++i)
            s += " " + vector[i].ToString("F" + digits).PadLeft(width) + Environment.NewLine;
        s += Environment.NewLine;
        return s;
    }

    static double[] VectorDuplicate(double[] vector)
    {
        double[] result = new double[vector.Length];
        for (int i = 0; i < vector.Length; ++i)
            result[i] = vector[i];
        return result;
    }

    static double[][] MatrixTranspose(double[][] matrix) // assumes matrix is not null
    {
        int rows = matrix.Length;
        int cols = matrix[0].Length; // assume all columns have equal size
        double[][] result = MatrixCreate(cols, rows); // note the indexing swap
        for (int i = 0; i < rows; ++i)
        {
            for (int j = 0; j < cols; ++j)
            {
                result[j][i] = matrix[i][j];
            }
        }
        return result;
    } // TransposeMatrix

    static double[][] MatrixProduct(double[][] matrixA, double[][] matrixB)
    {
        int aRows = matrixA.Length; int aCols = matrixA[0].Length;
        int bRows = matrixB.Length; int bCols = matrixB[0].Length;
        if (aCols != bRows)
            throw new Exception("Non-conformable matrices in MatrixProduct");

        double[][] result = MatrixCreate(aRows, bCols);

        for (int i = 0; i < aRows; ++i) // each row of A
            for (int j = 0; j < bCols; ++j) // each col of B
                for (int k = 0; k < aCols; ++k) // could use k < bRows
                    result[i][j] += matrixA[i][k] * matrixB[k][j];

        return result;
    } // MatrixProduct

    static double[] MatrixVectorProduct(double[][] matrix, double[] vector)
    {
        int mRows = matrix.Length; int mCols = matrix[0].Length;
        int vRows = vector.Length;
        if (mCols != vRows)
            throw new Exception("Non-conformable matrix and vector in MatrixVectorProduct");
        double[] result = new double[mRows]; // an n x m matrix times a m x 1 column vector is a n x 1 column vector
        for (int i = 0; i < mRows; ++i)
            for (int j = 0; j < mCols; ++j)
                result[i] += matrix[i][j] * vector[j];
        return result;
    }

    static double[][] MatrixInverse(double[][] matrix)
    {

        int n = matrix.Length;
        double[][] result = MatrixDuplicate(matrix);

        int[] perm;
        int toggle;
        double[][] lum = MatrixDecompose(matrix, out perm, out toggle);
        if (lum == null)
            return null;

        double[] b = new double[n];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < n; ++j)
            {
                if (i == perm[j])
                    b[j] = 1.0;
                else
                    b[j] = 0.0;
            }

            double[] x = HelperSolve(lum, b); // 

            for (int j = 0; j < n; ++j)
                result[j][i] = x[j];
        }
        return result;
    }

    // -------------------------------------------------------------------------------------------------------------------

    static double[] HelperSolve(double[][] luMatrix, double[] b) // helper
    {
        // solve Ax = b if you already have luMatrix from A and b has been permuted
        int n = luMatrix.Length;

        // 1. make a copy of the permuted b vector
        double[] x = new double[n];
        b.CopyTo(x, 0);

        // 2. solve Ly = b using forward substitution
        for (int i = 1; i < n; ++i)
        {
            double sum = x[i];
            for (int j = 0; j < i; ++j)
            {
                sum -= luMatrix[i][j] * x[j];
            }
            x[i] = sum;
        }

        // 3. solve Ux = y using backward substitution
        x[n - 1] /= luMatrix[n - 1][n - 1];
        for (int i = n - 2; i >= 0; --i)
        {
            double sum = x[i];
            for (int j = i + 1; j < n; ++j)
            {
                sum -= luMatrix[i][j] * x[j];
            }
            x[i] = sum / luMatrix[i][i];
        }

        return x;
    } // HelperSolve

    // -------------------------------------------------------------------------------------------------------------------

    static double[][] MatrixDecompose(double[][] matrix, out int[] perm, out int tog)
    {
        // Doolittle's method (1.0s on L diagonal) with partial pivoting
        int rows = matrix.Length;
        int cols = matrix[0].Length; // assume all rows have the same number of columns so just use row [0].
        if (rows != cols)
            throw new Exception("Attempt to MatrixDecompose a non-square mattrix");

        int n = rows; // convenience

        double[][] result = MatrixDuplicate(matrix); // make a copy of the input matrix

        perm = new int[n]; // set up row permutation result
        for (int i = 0; i < n; ++i) { perm[i] = i; }

        tog = 1; // toggle tracks number of row swaps. used by MatrixDeterminant

        double ajj, aij;

        for (int j = 0; j < n - 1; ++j) // each column
        {
            double max = Math.Abs(result[j][j]); // find largest value in row
            int pRow = j;
            for (int i = j + 1; i < n; ++i)
            {
                aij = Math.Abs(result[i][j]);
                if (aij > max)
                {
                    max = aij;
                    pRow = i;
                }
            }

            if (pRow != j) // if largest value not on pivot, swap rows
            {
                double[] rowPtr = result[pRow];
                result[pRow] = result[j];
                result[j] = rowPtr;

                int tmp = perm[pRow]; // and swap perm info
                perm[pRow] = perm[j];
                perm[j] = tmp;

                tog = -tog; // adjust the row-swap toggle
            }

            ajj = result[j][j];
            if (Math.Abs(ajj) < 0.00000001) // if diagonal after swap is zero . . .
                return null; // consider a throw

            for (int i = j + 1; i < n; ++i)
            {
                aij = result[i][j] / ajj;
                result[i][j] = aij;
                for (int k = j + 1; k < n; ++k)
                {
                    result[i][k] -= aij * result[j][k];
                }
            }
        } // main j loop

        return result;
    } // MatrixDecompose

}

