using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using AOT;

namespace MoodMe
{
    public class EmotionsInterface
    {
        private IntPtr _handler;
        private static MoodMeEmotions.MDMEmotions _detectedemotions;
        private float[] _buff;
        private DateTime _timestamp;

        public ManageEmotionsNetwork EmotionNetworkManager { get; set; }
        public FaceDetector FaceDetectorManager { get; set; }

        
        public EmotionsInterface(ManageEmotionsNetwork emotionNetworkManager, FaceDetector faceDetector)
        {
            EmotionNetworkManager = emotionNetworkManager;
            FaceDetectorManager = faceDetector;
        }


        public bool ProcessFrame()
        {

            //bool res = false;
            EmotionNetworkManager.Process = true;
            FaceDetectorManager.Process = true;
            return true;
        }


        public MoodMeEmotions.MDMEmotions DetectedEmotions
        {
            get
            {
                _buff = EmotionNetworkManager.GetCurrentEmotionValues;
                if (_buff != null)
                {
                    _detectedemotions = new MoodMeEmotions.MDMEmotions()
                    {
                        angry = _buff[0],
                        disgust = _buff[1],
                        scared = _buff[2],
                        happy = _buff[3],
                        sad = _buff[4],
                        surprised = _buff[5],
                        neutral = _buff[6],
                        latency = 0,
                        latency_avg = 0,
                        AllZero = (_buff[0] + _buff[1] + _buff[2] + _buff[3] + _buff[4] + _buff[5] + _buff[6]) == 0,
                        Error = false
                    };

                }
                return _detectedemotions;
            }
        }

        private static string GetLastTrackerError()
        {
            string s = "X";
            return (s);
        }
        public int SetLicense(string email, string key)
        {
            return 0x7fff;
        }
    }
}