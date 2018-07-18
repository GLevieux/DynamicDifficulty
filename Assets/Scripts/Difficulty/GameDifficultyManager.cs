using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Pour utiliser le manager de difficulté :
 * 1) appeler setPlayerId avec le nom du joueur et setActivity avec l'activité qu'on souhaite activer 
 *    les datas sont chargées et le modèle se met à jour automatiquement
 * 2) appeler getModelQuality pour savoir si le modèle est capable de prédire des choses.
 *    si c'est le cas (choisir un seuil), alors passer au point 3 puis 4
 * 3) appeler getDiffParams avec le bon numéro de niveau (on commence à 0 puis on incrémente à chaque fois
 *    que le joueur gagne ou perd le challenge0) pour avoir les paramètres de difficulté
 * 4) appeler addTry pour ajouter un nouvel essai du joueur au modèle et le sauver 
 */

[RequireComponent(typeof(GameDifficultyModel))]
public class GameDifficultyManager : MonoBehaviour {

    public enum GDActivityEnum
    {
        TANK
    }

    abstract class GDActivity
    {
        public GDActivityEnum ActivityEnum;
        public string Name;
        public int NbVars;
        public abstract double[] getParams(GameDifficultyModel model, double difficulty);
    }

    class TankActivity : GDActivity
    {
        public TankActivity()
        {
            Name = "Tank";
            NbVars = 1;
            ActivityEnum = GDActivityEnum.TANK;
        }

        public override double[] getParams(GameDifficultyModel model, double difficulty)
        {   
            double[] vars = new double[1];
            vars[0] = model.getDiffParameter(difficulty); //La variable 0 est set avec le modèle, toujours
            return vars;
        }
    }
    
    public void Awake()
    {
        Model = GetComponent<GameDifficultyModel>();
    }

    public Text DebugText;

    public AnimationCurve DifficultyCurveLearning; //Courbe au début progressive
    public int NbStepsLearning; //Nombre d'essais pour l'apprentissage
    public AnimationCurve [] DifficultyCurvePlaying; //Courbe en jeu, qu'on répète, après l'apprentissage
    public int NbStepsPlaying; //Nombre d'essais pour le jeu (on répète, sert à scaler la courbe)
    private int DiffCurvePlayingChosen = 0; //La courbe de difficulté qu'on utiliser pour la phase après learning
    private float Exploration = 0.05f; //Exploration appliquée à la difficulté quand on utilise la régression
    private float DeltaWin = 0.1f; 
    private float DeltaFail = 0.1f;
    private bool UsingLRModel = false;
    private double TargetDiff = 0;

    GameDifficultyModel Model;
    GDActivity Activity;
    private string PlayerId = "UnknownPlayer";

    private void setDiffCurve(int diffCurve)
    {
        DiffCurvePlayingChosen = diffCurve;
    }

    /**
     * Permet de sélectionner le bon joueur
     */
    public void setPlayerId(string playerId)
    {
        PlayerId = playerId;
    }

    public string getPlayerId()
    {
        return PlayerId;
    }

    /**
        * Permet de sélectionner l'activite à débuter
        * On met à jour le modèle et la courbe de difficulté
        */
    public void setActivity(GDActivityEnum activity)
    {
        //Si c'est la meme, on ne touche a rien
        if (Activity != null && activity == Activity.ActivityEnum)
            return;

        switch (activity)
        {
            case GDActivityEnum.TANK: Activity = new TankActivity(); break;
            default: break;
        }

        //On met à jour le profil
        Model.setProfile(PlayerId, Activity.Name);

        //On tire une courbe de difficulté au hasard
        setDiffCurve(Random.Range(0, DifficultyCurvePlaying.Length));
    }

    /**
     * Retourne la qualité du modèle entre 0 et 1
     * A partir d'un certain seuil, on peut choisir d'utiliser les valeurs que donne le modèle
     * Sinon on se contente de continuer à lui donner des données et on utilise
     * une autre stratégie pour déterminer la difficulté
     */
    public double getModelQuality()
    {
        return Model.getModelQuality();
    }
    
    /**
     * Ajoute un essai au modèle et le sauve
     * diffVars : valeur des paramètres du challenge que le joueur vient de tenter de réussir
     *            utiliser toujours le même ordre pour les différents paramètres d'un meme challenge
     * win : si le joueur a réussi ou pas son essai
     */
    public void addTry(double[] diffVars, bool win)
    {
        Model.addTry(diffVars, win, true, true);
    }

    /**
     * Donne la valeur des paramètres de l'activité,
     * en fonction du numéro de niveau.
     * On part du niveau 0, et à chaque nouveau challenge, demander
     * par exemple un nouveau niveau incrémenté.
     */
    public double[] getDiffParams(int numLevel)
    {
        double[] retVals = null;

        double quality = Model.getModelQuality();
        string debugString = "Qualité: " + Mathf.Floor((float)quality*100)/100;

        if(quality < 0.6)
        {
            Debug.Log("Model quality is low (" + quality + "), using +-(delta * rnd(0.5,1.0)) based on win / fail");
            //Recup les derniers essais
            double [] lastTryAndRes = Model.getLastTryAndRes();
            retVals = new double[Activity.NbVars];

            //Si on est au toiut début, on part de 0, la diff la plus basse
            if (numLevel == 0 || lastTryAndRes == null)
            {
                for (int i = 0; i < retVals.Length; i++)
                    retVals[i] = 0;
            }
            else
            {
                bool win = lastTryAndRes[lastTryAndRes.Length - 1] > 0;
                
                float delta = win ? DeltaWin : -DeltaFail;
                for (int i = 0; i < retVals.Length; i++)
                    retVals[i] = lastTryAndRes[i] + (delta * Random.Range(0.5f, 1f));
            }
            UsingLRModel = false;
            TargetDiff = 0.5f;
        }
        else
        {
            Debug.Log("Model is okay (" + quality + "), using it :)");

            //on regarde dans quelle courbe on tombe
            AnimationCurve ac = DifficultyCurveLearning;
            int numStepInCurve = numLevel;
            int nbStepOfCurve = NbStepsLearning;
            if (numLevel >= NbStepsLearning)
            {
                ac = DifficultyCurvePlaying[DiffCurvePlayingChosen];
                numStepInCurve -= NbStepsLearning;
                numStepInCurve = numStepInCurve % NbStepsPlaying;
                nbStepOfCurve = NbStepsPlaying;
            }



            //On récup la difficulté voulue
            double difficulty = ac.Evaluate((float)numStepInCurve / (float)nbStepOfCurve);
            difficulty = System.Math.Max(System.Math.Min(difficulty, 1), 0);

            //On rajoute un petit delta en fonction de l'exploration
            double explo = Random.Range(-Exploration, Exploration);

            debugString += "\nExplo: " + Mathf.Floor((float)explo * 100) / 100;

            //On set la target diff
            TargetDiff = Mathf.Clamp01((float)(difficulty + explo));

            //On affiche la difficulté voulue
            Debug.Log("Target difficulty is " + TargetDiff);

            debugString += "\nDiff: " + Mathf.Floor((float)TargetDiff * 100) / 100;
            
            //On construit le tableau en fonction de l'activité
            retVals = Activity.getParams(Model, TargetDiff);
        }

        //On cap les paramètres entre 0 et 1
        for (int i = 0; i < retVals.Length; i++)
            retVals[i] = System.Math.Max(System.Math.Min(retVals[i],1),0);

        string parsStr = "Player : "+PlayerId+"\n";
        for (int i = 0; i < retVals.Length; i++)
            parsStr += i + ":[ " + (Mathf.Floor((float)(retVals[i]) * 100) / 100) + " ]  ";
        Debug.Log("Giving params "+parsStr);

        debugString += "\n"+parsStr;
        DebugText.text = debugString;

        UsingLRModel = true;

        return retVals;
    }


    //Pour faire du log et espionner les variables du modèle
    public double [] getBetas()
    {
        return Model.getBetas();
    }

    public bool isUsingLRModel()
    {
        return UsingLRModel;
    }

    public double getTargetDiff()
    {
        return TargetDiff;
    }


}
