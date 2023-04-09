// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;

namespace Mediapipe.Unity
{
  public abstract class ImageSourceSolution<T> : Solution where T : GraphRunner
  {
    [SerializeField] protected Screen screen;
    [SerializeField] protected T graphRunner;
    [SerializeField] protected TextureFramePool textureFramePool;

    private Coroutine _coroutine;

    public RunningMode runningMode;

    public long timeoutMillisec
    {
      get => graphRunner.timeoutMillisec;
      set => graphRunner.timeoutMillisec = value;
    }

    public override void Play()
    {
      if (_coroutine != null)
      {
        Stop();
      }
      base.Play();
            Debug.Log("start coroutine");
      _coroutine = StartCoroutine(Run());
    }

    public override void Pause()
    {
      base.Pause();
      ImageSourceProvider.ImageSource.Pause();
    }

    public override void Resume()
    {
      base.Resume();
      var _ = StartCoroutine(ImageSourceProvider.ImageSource.Resume());
    }

    public override void Stop()
    {
      base.Stop();
      StopCoroutine(_coroutine);
      ImageSourceProvider.ImageSource.Stop();
      graphRunner.Stop();
    }

    private IEnumerator Run()
    {
            //Debug.Log("debug at Run()0");
            var graphInitRequest = graphRunner.WaitForInit(runningMode);
      var imageSource = ImageSourceProvider.ImageSource;
            //Debug.Log("debug at Run()1");
            yield return imageSource.Play();
            //Debug.Log("debug at Run()2");
            if (!imageSource.isPrepared)
      {
        Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
      textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
      SetupScreen(imageSource);
            //Debug.Log("debug at Run()3");
            yield return graphInitRequest;
      if (graphInitRequest.isError)
      {
        Logger.LogError(TAG, graphInitRequest.error);
                //Debug.Log("debug at Run()4 break");
                yield break;
      }
            //Debug.Log("debug at Run()5");
            OnStartRun();
      graphRunner.StartRun(imageSource);

      var waitWhilePausing = new WaitWhile(() => isPaused);

      while (true)
      {
        if (isPaused)
        {
          yield return waitWhilePausing;
        }

        if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }
        //Debug.Log("debug at Run()6");
                // Copy current image to TextureFrame
        ReadFromImageSource(imageSource, textureFrame);
        AddTextureFrameToInputStream(textureFrame);
        yield return new WaitForEndOfFrame();
        //Debug.Log("debug at Run()7");

        if (runningMode.IsSynchronous())
        {
          RenderCurrentFrame(textureFrame);
          //Debug.Log("debug at Run()8");
          yield return WaitForNextValue();
          //Debug.Log("debug at Run()9");
        }
      }
    }

    protected virtual void SetupScreen(ImageSource imageSource)
    {
      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);
    }

    protected virtual void RenderCurrentFrame(TextureFrame textureFrame)
    {
      screen.ReadSync(textureFrame);
    }

    protected abstract void OnStartRun();

    protected abstract void AddTextureFrameToInputStream(TextureFrame textureFrame);

    protected abstract IEnumerator WaitForNextValue();
  }
}
