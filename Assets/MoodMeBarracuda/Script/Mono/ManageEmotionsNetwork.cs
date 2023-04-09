using Unity;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System;
using System.Linq;
using UnityEngine.UI;
using static MoodMe.FaceDetectorPostProcessing;

namespace MoodMe
{
    public class ManageEmotionsNetwork : MonoBehaviour
    {

        public NNModel EmotionsNNetwork;
        public NNModel BadEmotionsNNetwork;
        public NNModel SadEmotionsNNetwork;



        public int ImageNetworkWidth = 48;
        public int ImageNetworkHeight = 48;

        [Range(1, 4)]
        public int ChannelCount = 1;

        public WorkerFactory.Device Device = WorkerFactory.Device.CPU;

        public bool Process;

        //public GameObject PreviewEmotionsPlane;

        //public RawImage PreviewEmotions;

        public float[] GetCurrentEmotionValues
        {
            get
            {
                return _detectedEmotions.Values.ToArray();
            }
        }


        private static Dictionary<string, float> _detectedEmotions;

        //private static MoodMeEmotions.MDMEmotions CurrentEmotions;

        private IWorker _engine;
        private IWorker _engine2;
        private IWorker _engine3;

        private string[] _emotionsLabelFull = { "Angry", "Disgusted", "Scared", "Happy", "Sad", "Surprised", "Neutral" };

        //private string[] EmotionsLabel = { "Angry", "Disgusted", "Scared", "Happy", "Sad", "Surprised", "Neutral" };             
        //private string[] EmotionsLabel = { "Neutral", "Surprised", "Sad" };
        private string[] _emotionsLabelGood = { "Neutral", "Happy", "Surprised", "Sad" };
        private string[] _emotionsLabelBad = { "Angry", "Disgusted", "Scared" };
        //private string[] EmotionsLabel = { "Neutral", "Angry", "Disgusted", "Scared" };


        private Tensor _tensor;
        private Tensor _output;
        private Tensor _output2;
        private Tensor _output3;

        private Color32[] _rgba;
        private float[] _tensorData;

        // Start is called before the first frame update
        void Start()
        {

            Model model = ModelLoader.Load(EmotionsNNetwork);
            Model model2 = ModelLoader.Load(BadEmotionsNNetwork);
            Model model3 = ModelLoader.Load(SadEmotionsNNetwork);
            _engine = WorkerFactory.CreateWorker(model, Device);
            _engine2 = WorkerFactory.CreateWorker(model2, Device);
            _engine3 = WorkerFactory.CreateWorker(model3, Device);

            _detectedEmotions = new Dictionary<string, float>();

            foreach (string key in _emotionsLabelFull)
            {
                _detectedEmotions.Add(key, 0);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!Process) return;
            if (FaceDetector.OutputCrop == null) return;
            if (!(FaceDetector.OutputCrop.Length == (ImageNetworkWidth * ImageNetworkHeight))) return;
            Texture2D previewTexture;
            previewTexture = new Texture2D(ImageNetworkWidth, ImageNetworkHeight, TextureFormat.R8, false); // width, height 가 각각 48, 48


            _rgba = FaceDetector.OutputCrop;

            previewTexture.SetPixels32(_rgba);
            previewTexture.Apply();


            //if (PreviewEmotionsPlane != null) PreviewEmotionsPlane.GetComponent<MeshRenderer>().material.mainTexture = previewTexture;
            //if (PreviewEmotions != null) PreviewEmotions.texture = previewTexture;

            _tensor = new Tensor(previewTexture);
            DateTime timestamp;


            timestamp = DateTime.Now;
            _output = _engine.Execute(_tensor).CopyOutput();
            _output2 = _engine2.Execute(_tensor).CopyOutput();
            _output3 = _engine3.Execute(_tensor).CopyOutput();
            //Debug.Log("EMOTIONS INFERENCE TIME: " + (DateTime.Now - timestamp).TotalMilliseconds + " ms");
            float[] results = _output.data.Download(_output.shape);
            float[] results2 = _output2.data.Download(_output2.shape);
            float[] results3 = _output3.data.Download(_output3.shape);
            //float check = 0;
            ////Debug.Log(results[1]);
            //for (int i = 0; i > 4; i++)
            //{
            //    check = results[i];
            //    check.ToString();
            //    Debug.Log(check);
            //}

            //for (int i = 0; i < results.Length; i++)
            //{
            //    _detectedEmotions[_emotionsLabelGood[i]] = results[i];
            //    //Debug.Log(EmotionsLabel[i] + " = " + results[i]);
            //}


            for (int i = 0; i < results.Length - 1; i++)
            {
                _detectedEmotions[_emotionsLabelGood[i]] = results[i];
            }

            for (int i = 1; i < results2.Length; i++)
            {
                _detectedEmotions[_emotionsLabelBad[i - 1]] = results2[i];
            }

            _detectedEmotions[_emotionsLabelGood[3]] = (results3[3] * 1.8f + results[3] * 0.2f) / 2;
            //_detectedEmotions[_emotionsLabelGood[3]] = results3[3];





            //Debug.Log("-------------------------------------------");

            _output.Dispose();
            _output2.Dispose();
            _output3.Dispose();
            _tensor.Dispose();
            Process = false;
        }

        void OnDestroy()
        {
            _output.Dispose();
            _output2.Dispose();
            _output3.Dispose();
            _tensor.Dispose();
            _engine.Dispose();
            _engine2.Dispose();
            _engine3.Dispose();
        }





    }

}