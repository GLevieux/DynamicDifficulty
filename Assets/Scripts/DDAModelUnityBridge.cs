using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDAModelUnityBridge : MonoBehaviour {

    public DDAModel DdaModel;
    public string PlayerId = "UnknownPlayer";
    public string PlayerAge = "UnknownAge";
    public string PlayerGender = "UnknownGender";
    public string ChallengeId = "UnknownChallenge";
    public float ThetaStart = 0.2f;
    public bool DoNotUpdateAccuracy = false;
    DDAModel.DDAAlgorithm Algorithm;
    
    public void setPlayerId(string playerId)
    {
        PlayerId = playerId;
        DdaModel.PlayerId = PlayerId;
    }

    public void setPlayerAge(string playerAge)
    {
        PlayerAge = playerAge;
        DdaModel.PlayerAge = PlayerAge;
    }

    public void setPlayerGender(string playerGender)
    {
        PlayerGender = playerGender;
        DdaModel.PlayerGender = PlayerGender;
    }

    public void setChallengeId(string challengeId)
    {
        ChallengeId = challengeId;
        DdaModel.ChallengeId = ChallengeId;
    }

    public string getPlayerId()
    {
        return PlayerId;
    }

    public string getPlayerAge()
    {
        return PlayerAge;
    }

    public string getPlayerGender()
    {
        return PlayerGender;
    }

    public string getChallengeId()
    {
        return ChallengeId;
    }

    public void setDDAAlgorithm(DDAModel.DDAAlgorithm algorithm)
    {
        Algorithm = algorithm;
        DdaModel.setDdaAlgorithm(algorithm);
    }

    public DDAModel.DDAAlgorithm getDDAAlgorithm()
    {
        return DdaModel.getDdaAlgorithm();
    }

    public void initPMAlgorithm(double lastTheta, bool wonLastTime = false)
    {
        DdaModel.setPMInit(lastTheta, wonLastTime);
    }
        
    void Awake () {
        DdaModel  = new DDAModel(new DDADataManagerLocalCSV(), PlayerId, PlayerAge, PlayerGender, ChallengeId);
        DdaModel.setDdaAlgorithm(Algorithm);
        initPMAlgorithm(ThetaStart);
    }

    public void addLastAttempt(DDADataManager.Attempt attempt)
    {
        DdaModel.addLastAttempt(attempt);
    }

    public DDAModel.DiffParams computeNewDiffParams(double targetDifficulty = 0, bool doNotUpdateLRAccuracy = false)
    {
        DdaModel.setDdaAlgorithm(Algorithm);
        return DdaModel.computeNewDiffParams(targetDifficulty, doNotUpdateLRAccuracy || DoNotUpdateAccuracy);
    }

    //For test purpose only
    public bool checkDataAgainst(List<DDADataManager.Attempt> attempts)
    {
        return DdaModel.checkDataAgainst(attempts);
    }
}

