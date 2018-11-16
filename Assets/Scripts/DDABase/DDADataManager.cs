using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class DDADataManager
{
    /**
     * Represents an attempt to win a challenge 
     */
    public class Attempt
    {
        public double[] Thetas; //Variable describing challenge difficulty
        public double Result;//1 if player won this challenge, 0 if not

        public bool IsSame(object obj) //Not using equals because dont want to mess with Equals and hashcodes, object not immutable (should be ?)
        {
            var other = obj as Attempt;

            if (other == null)
                return false;

            if(!(other.Thetas == null && Thetas == null))
            {
                if (other.Thetas == null)
                    return false;
                if (Thetas == null)
                    return false;

                double delta = 0;
                for (int i = 0; i < Thetas.Length; i++)
                    delta += System.Math.Abs(Thetas[i] - other.Thetas[i]);
                if (delta / Thetas.Length > 0.00001)
                    return false;
            }

            if (Result != other.Result)
                return false;

            return true;
        }
    }
    
    //Save all these new attempts for this player and this challenge
    public abstract void addAttempt(string playerId, string challengeId, Attempt attempt);
    //Get nbLastAttempts of this player for this challenge
    public abstract List<Attempt> getAttempts(string playerId, string challengeId, int nbLastAttempts);
}
