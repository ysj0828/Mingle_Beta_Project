using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

namespace Mediapipe.Unity.Tutorial
{

    public class WaitForThreadedTask : UnityEngine.CustomYieldInstruction
    {

        private bool isRunning;

        public WaitForThreadedTask(Action task)
        {
            isRunning = true;
            new Thread(() => { task(); isRunning = false; }).Start();
        }

        public override bool keepWaiting { get { return isRunning; } }
    }
}