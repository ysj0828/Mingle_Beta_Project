using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Unity;

namespace Mediapipe.Unity.Tutorial
{
    public class AllVariables : MonoBehaviour
    {
        public static AllVariables instance = null;

        public NormalizedLandmark topLeftEye;
        public NormalizedLandmark bottomLeftEye;
        public NormalizedLandmark leftLeftEye;
        public NormalizedLandmark rightLeftEye;
        public NormalizedLandmark topRightEye;
        public NormalizedLandmark bottomRightEye;
        public NormalizedLandmark leftRightEye;
        public NormalizedLandmark rightRightEye;
        public NormalizedLandmark centerLeftEyebrow;
        public NormalizedLandmark centerRightEyebrow;
        public NormalizedLandmark topNose;
        public NormalizedLandmark middleNose;
        public NormalizedLandmark bottomNose;
        public NormalizedLandmark topMouth;
        public NormalizedLandmark bottomMouth;
        public NormalizedLandmark bottomLip;
        public NormalizedLandmark LeftMouth;
        public NormalizedLandmark RightMouth;
        public NormalizedLandmark CenterTopMouth;
        public NormalizedLandmark CenterBottomMouth;
        public NormalizedLandmark LeftEar;
        public NormalizedLandmark RightEar;
        public NormalizedLandmark Nose;
        public NormalizedLandmark Jaw;
        public NormalizedLandmark leftIris;
        public NormalizedLandmark rightIris;
        public NormalizedLandmark belowLeftMouth;
        public NormalizedLandmark belowRightMouth;

        void Awake()
        {
            if (instance == null)
                instance = this;

            else Destroy(this);
        }
    }
}