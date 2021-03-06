﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public delegate void Complete(float ratingLevel);
    public static Complete OnComplete;
    public delegate void HourChange(int time);
    public static HourChange OnHourChange;

    public KeyPrompts keyPrompts;
    public CheckFare checkFare;
    public AudioSource busBG;

    readonly int START_HOUR = 6;
    readonly int HOURS_IN_DAY = 12; // 06:00 - 18:00
    readonly int DAY_IN_REAL_MINUTES = 1;

    float scoreToday;
    float targetTime;
    float timeElapsed;
    int currentHour;
    float gameHoursPerSecond;
    // 0: 6am - 9am
    // 1: 9am - 12pm
    // 2: 12pm - 3pm
    // 3: 3pm - 6pm
    List<float> fareRates;
    int fareRateIndex;

    private void OnEnable() {
        OnComplete += CompleteEvent;
    }

    private void OnDisable() {
        OnComplete -= CompleteEvent;
    }


    private void Start() {
        LoadDay();
    }

    public void LoadDay() {
        scoreToday = 0;
        fareRateIndex = 0;
        InitTimer(DAY_IN_REAL_MINUTES, HOURS_IN_DAY);
        LoadFareRates();
        StartCoroutine(StartDay());
        StartCoroutine(RushHour());
        StartCoroutine(UpdateHour());

        keyPrompts.Init();
        checkFare.Init();
        BusEvent.OnNewDay?.Invoke();
        DrivingPrompt.OnHide?.Invoke();
        GameManager.OnShowBusOverlay?.Invoke();
        ScoreRating.OnUpdateScore?.Invoke(scoreToday);
        CheckFare.OnUpdateFare?.Invoke(fareRates[fareRateIndex]);
        RoadLines.OnInit?.Invoke();
        Buildings.OnInit?.Invoke();
        Trees.OnInit?.Invoke();
    }

    void LoseDay() {
        keyPrompts.Stop();
        checkFare.Stop();
        ScoreRating.OnUpdateScore?.Invoke(scoreToday);
        GameManager.OnShowLoseScreen?.Invoke();
    }

    void CompleteDay() {
        busBG.Stop();
        keyPrompts.Stop();
        checkFare.Stop();
        GameManager.OnShowResults?.Invoke();
    }

    void CompleteEvent(float ratingLevel) {
        scoreToday += ratingLevel;
        if (scoreToday < 0) {
            scoreToday = 0;
        }
        ScoreRating.OnUpdateScore?.Invoke(scoreToday);
    }

    IEnumerator StartDay() {
        busBG.Play();
        while (timeElapsed <= targetTime) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        CompleteDay();
    }

    void InitTimer(float totalRealMinutes, int totalGameHours) {
        targetTime = 60 * totalRealMinutes; // convert to seconds
        timeElapsed = 0;
        currentHour = START_HOUR;
        gameHoursPerSecond = targetTime / totalGameHours;
    }

    IEnumerator UpdateHour() {
        int hourCounter = 0; // track when to increment fareRateIndex

        while (timeElapsed <= targetTime) {
            if (hourCounter == 3) {
                fareRateIndex++;
                hourCounter = 0;
                CheckFare.OnUpdateFare?.Invoke(fareRates[fareRateIndex]);
            }
            OnHourChange?.Invoke(currentHour);
            //FarePoster.OnHighlightFare?.Invoke(fareRateIndex);
            currentHour++;
            hourCounter++;
            yield return new WaitForSeconds(gameHoursPerSecond);
        }
    }

    IEnumerator RushHour() {
        while (timeElapsed <= targetTime) {
            if (currentHour == 9) {
                BusEvent.OnRushHourStart?.Invoke();
                yield return new WaitUntil(() => currentHour == 11);
                BusEvent.OnRushHourEnd?.Invoke();
            }

            if (currentHour == 16) {
                BusEvent.OnRushHourStart?.Invoke();
                yield return new WaitUntil(() => currentHour == 18);
                BusEvent.OnRushHourEnd?.Invoke();
            }
            yield return null;
        }
    }

    void LoadFareRates() {
        fareRates = new List<float>();
        fareRateIndex = 0;
        for (int i = 0; i < 4; i++) {
            float fare = Random.Range(1f, 3f);
            fare -= (float)( fare % 0.01 ); // 2 decimal places
            fare = Mathf.Round(fare * 10f) / 10f;
            fareRates.Add(fare);
        }
        FarePoster.OnUpdateFare?.Invoke(fareRates[0], fareRates[1], fareRates[2], fareRates[3]);
    }
}
