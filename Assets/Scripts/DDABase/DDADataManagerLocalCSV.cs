using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using UnityEngine;

public class DDADataManagerLocalCSV : DDADataManager {

    string FileDataName = "data.csv";

    class CacheData
    {
        public List<Attempt> Attempts = new List<Attempt>();
        public string PlayerId;
        public string ChallengeId;
        public int SizeLimit = 1000;
        
        public CacheData(string playerId, string challengeId, int sizeLimit = 1000)
        {
            PlayerId = playerId;
            ChallengeId = challengeId;
            SizeLimit = sizeLimit;
        }

        public void addAttempt(Attempt attempt)
        {
            Attempts.Add(attempt);
            if (Attempts.Count > SizeLimit)
                Attempts.RemoveRange(0, Attempts.Count - SizeLimit);
        }
    }

    List<CacheData> Caches = new List<CacheData>();


    CacheData findCache(string playerId, string challengeId)
    {
        CacheData cache = null;
        foreach (CacheData lcache in Caches)
        {
            if (lcache.PlayerId == playerId && lcache.ChallengeId == challengeId)
                cache = lcache;
        }
        return cache;
    }

    CacheData createCache(string playerId, string challengeId, int sizeLimit)
    {
        CacheData cache = findCache(playerId, challengeId);
      
        if (cache == null)
        {
            cache = new CacheData(playerId, challengeId, sizeLimit);
            Caches.Add(cache);
        }

        return cache;
    }

    void deleteCache(string playerId, string challengeId)
    {
        Caches.RemoveAll(item => item.PlayerId == playerId && item.ChallengeId == challengeId);
    }


    //Save all these new attempts for this player and this challenge
    public override void addAttempt(string playerId, string challengeId, Attempt attempt)
    {
        //On va stocker les donnees en cache
        CacheData cache = findCache(playerId, challengeId);
        if(cache != null) //Sinon il sera créé au load, ou on a la taille limite pour dimensionner le cache
            cache.addAttempt(attempt);

        //On sauve
        string csvFile = Application.persistentDataPath + "/" + playerId + "_" + challengeId + FileDataName;

        try
        {
            FileStream ofs = new FileStream(csvFile, FileMode.Append);
            StreamWriter sw = new StreamWriter(ofs);
            
            for (int i = 0; i < attempt.Thetas.Length; i++)
            {
                sw.Write(attempt.Thetas[i]);
                sw.Write(";");
            }
            sw.Write(attempt.Result);
            sw.Write("\n");
            
            sw.Flush();
            ofs.Flush();
            sw.Close();
            ofs.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    //Get nbLastAttempts of this player for this challenge
    public override List<Attempt> getAttempts(string playerId, string challengeId, int nbLastAttempts)
    {
        //On va stocker les donnees en cache
        CacheData cache = findCache(playerId, challengeId);

        if(cache != null)
        {
            if(cache.SizeLimit == nbLastAttempts)
            {
                //On a deja les données en cache et c'est la bonne taille, on les retourne
                return cache.Attempts;
            }
            else
            {
                //Pas la meme taille, on va recharger tout le fichier
                Debug.Log("Warning !! you need to always retrive the same number of attempts for performancy reasons. If cache size changes, file need to be loaded again.");
                deleteCache(playerId, challengeId);
                cache = null;
            }

        }
        
        //On a pas les données en cache, on crée un nouveau cache
        cache = createCache(playerId, challengeId, nbLastAttempts);

        string csvFile = Application.persistentDataPath + "/" + playerId + "_" + challengeId + FileDataName;

        //Counting number of lines and variables
        try
        {
            FileStream ifs = new FileStream(csvFile, FileMode.Open);
            StreamReader sr = new StreamReader(ifs);
            string line = "";
            string[] tokens = null;
            int ct = 0;
            int nbVars = -1;
            bool bHeaders = false;
            while ((line = sr.ReadLine()) != null) 
            {
                //For first line, test if headers
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
                    nbVars--; //Removing dependant variable
                }
                ++ct;
            }

            if (bHeaders)
                ct--;

            sr.Close(); ifs.Close();


            //On parse le fichier pour charger les datas
            ifs = new FileStream(csvFile, FileMode.Open);
            sr = new StreamReader(ifs);
            int row = 0;

            while ((line = sr.ReadLine()) != null)
            {
                if(row >= (ct - nbLastAttempts))
                {
                    line = line.Trim();
                    tokens = line.Split(';');
                    Attempt attempt = new Attempt();
                    attempt.Thetas = new double[nbVars];
                    for (int i = 0; i < nbVars; i++)
                        attempt.Thetas[i] = double.Parse(tokens[i]);
                    attempt.Result = double.Parse(tokens[tokens.Length - 1]);
                    cache.addAttempt(attempt);
                }                
                ++row;
            }
            sr.Close(); ifs.Close();
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("File " + csvFile + " not found (" + e.Message + ")");
        }
        catch (IsolatedStorageException e)
        {
            Console.WriteLine("File " + csvFile + " not found (" + e.Message + ")");
        }

        return cache.Attempts;
    }

}
