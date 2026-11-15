using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jointure.Samples
{
    public class Stopwatch : MonoBehaviour
    {
        private List<Text> _elapsedTexts = new List<Text>(), _bestTexts = new List<Text>();

        [Tooltip("Leave blank to not save")]
        [SerializeField]
        private string _saveName;

        private float _elapsedTime;
        private TimeSpan _timeSpan;
        private bool _isTimerGoing;
        private float _record;

        private void Start()
        {
            foreach (Transform gui in transform)
            {
                _elapsedTexts.Add(gui.GetChild(0).Find("Elapsed").GetComponent<Text>());
                _bestTexts.Add(gui.GetChild(0).Find("Best").GetComponent<Text>());
            }

            ShowStopwatch();

            if (_saveName == "")
            {
                foreach (Text bestText in _bestTexts)
                    bestText.text = "";

                return;
            }

            _record = PlayerPrefs.GetFloat(_saveName, 0f);

            ShowRecord(false);
        }

        private IEnumerator UpdateTimer()
        {
            while (_isTimerGoing)
            {
                _elapsedTime += Time.unscaledDeltaTime;
                ShowStopwatch();
                yield return null;
            }

            if (_saveName != "")
            {
                bool hasNewBest = false;

                if (_elapsedTime < _record || _record == 0f)
                {
                    PlayerPrefs.SetFloat(_saveName, _elapsedTime);
                    _record = _elapsedTime;
                    hasNewBest = true;
                }

                ShowRecord(hasNewBest);
            }
        }

        public void StartStopwatch()
        {
            if (_isTimerGoing)
                return;

            _isTimerGoing = true;
            StartCoroutine(UpdateTimer());
        }

        public void EndStopwatch()
        {
            _isTimerGoing = false;
        }

        public void ResetStopwatch()
        {
            _elapsedTime = 0f;

            ShowStopwatch();
            ShowRecord(false);
        }

        private void ShowStopwatch()
        {
            _timeSpan = TimeSpan.FromSeconds(_elapsedTime);

            foreach (Text timeText in _elapsedTexts)
                timeText.text = _timeSpan.ToString("mm':'ss'.'ff");
        }

        private void ShowRecord(bool hasNewBest)
        {
            foreach (Text recordText in _bestTexts)
                recordText.text = "NO PERSONAL BEST";

            if (_record == 0f)
                return;

            _timeSpan = TimeSpan.FromSeconds(_record);

            string message = "PERSONAL BEST: ";
            if (hasNewBest)
            {
                message = "NEW BEST: ";

                foreach (Text timeText in _elapsedTexts)
                    timeText.text = "slay queen";
            }

            foreach (Text personalBestText in _bestTexts)
                personalBestText.text = message + _timeSpan.ToString("mm':'ss'.'ff");
        }
    }
}
