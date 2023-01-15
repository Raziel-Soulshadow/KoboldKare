using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class GaussianTest : MonoBehaviour
{
    public bool doTest = false, doMany = false;
    public float min = 0f, max = 1f, check = .5f;
    public int count = 0, avg = 0, avgCount = 0;

    void Update()
	{
        if (doTest) { Debug.Log("Random = " + Mathf.Clamp(RandomGaussian(min, max), 0f, 1f)); doTest = false; }
        if (doMany) 
        {
            do { check = Mathf.Clamp(RandomGaussian(min, max), 0f, 1f); count++; } while (check > 0 && check < 1);
            Debug.Log("Took " + count + " tries to exceed clamp");
            avg = avg + count;
            avgCount++;
            doMany = false;
            count = 0;
            check = .5f;
            Debug.Log("Avg result is now " + (avg / avgCount));
        }
    }

    private float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f) {
        float u, v, S;
        do {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;      //value can be between 0 to 1(2), not including 1
        } while (S >= 1.0f);
        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);   //value can be between u * (infinity to 0, not including 0)
        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;   //assuming standard min/max, mean is .5
        float sigma = (maxValue - mean) / 3.0f;   //again assuming standard, sigma is 1/6th
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue); //so result is anywhere between... neg infinity to infinity, clamped by min/max. 
    }

    
}

