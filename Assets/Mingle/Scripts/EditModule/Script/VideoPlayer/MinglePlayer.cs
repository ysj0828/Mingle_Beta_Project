using System.Collections;
using System.Collections.Generic;
using com.palantiri.unity.videoplayer;
using UnityEngine;
using UnityEngine.Video;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Mingle;
using Photon.Pun;
using Vuplex.WebView;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System;
using UnityEngine.SceneManagement;
using static System.Net.WebRequestMethods;

public class MinglePlayer : MonoBehaviour, IPlayerEventListener
{
    [HideInInspector]
    public VideoPlayer _videoPlayer;
    RenderTexture _videoRenderTexture;
    Material _videoPlayerMaterial;
    Material[] _defaultMaterials;
    Material[] _playerMaterials = null;
    MeshRenderer _currentMeshRenderer = null;

    // Android exoPlayer
    private SimpleExoPlayer _exoPlayer = null;
    private bool initialized = false;
    // private Texture2D videoTexture = null;

    public Shader videoTextureShader;
    public Shader frameTextureShader;
    public MeshRenderer videoMeshRenderer;
    public bool IOSversion = true;

    private int _exoPlayerState = -1;

    //VuPlex
    public BaseWebViewPrefab WebViewPrefab;
    private BaseWebViewPrefab _webViewObject = null;

    [SerializeField] private PhotonView _photonView;

    static readonly string VIDEOLIST = "VIDEOLIST";
    static readonly string VIDEOINDEX = "VIDEOINDEX";

    private string Url = "https://custom0923.s3.ap-northeast-2.amazonaws.com/uploads/10Video.mp4";

    private bool _isYoutube = false;
    private bool _isOpenPage = false;

    private void Awake()
    {
        Web.SetUserAgent(true);
    }

    private async void Start()
    {
        _photonView = GetComponentInParent<PhotonView>();

        // _webViewObject = Instantiate(WebViewPrefab, GameObject.Find("WebViewBase").transform);
        // await _webViewObject.WaitUntilInitialized();
        initVersionCheck();
        //* 마스터에게 현재 비디오 요청 


#if UNITY_EDITOR || !UNITY_ANDROID
        initVideoPlayer();
#elif UNITY_ANDROID && !UNITY_EDITOR
        initExoPlayer();
#endif

        if (SceneManager.GetActiveScene().name == "ChatRoom")
        {
            _photonView.RPC("RequestVideo", RpcTarget.MasterClient);
            //* 마스터에게 현재 비디오 Time 요청
            _photonView.RPC("RequestVideoTime", RpcTarget.MasterClient, IOSversion);
        }
    }
    private void initVersionCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        IOSversion = false;
#elif UNITY_IOS || UNITY_EDITOR
        IOSversion = true;
#endif
    }
    public void onIsLoadingChanged(bool isLoading) { }
    public void onIsPlayingChanged(bool isPlaying) { }

    //* Android MP4 Video End Event
    public void onPlaybackStateChanged(int state)
    {
        _exoPlayerState = state;
        if (state == 4)
        {
            if (getPlayer().isPlaying() || !PhotonNetwork.IsMasterClient) return;
            Debug.Log("AndroidVideoEnd");
            PlayNext();
        }
    }
    public void onPlaybackSuppressionReasonChanged(int playbackSuppressionReason) { }
    public void onRepeatModeChanged(int repeatMode) { }
    public void onShuffleModeEnabledChanged(bool shuffleModeEnabled) { }
    public void onPositionDiscontinuity(int reason) { }
    public void onPlayWhenReadyChanged(bool playWhenReady, int reason) { }
    public void onPlayerError(ExoPlayerTypes.ExoPlaybackException error) { }
    public void onPlaybackParametersChanged(ExoPlayerTypes.PlaybackParameters playbackParameters) { }
    public void onMediaItemTransition(ExoPlayerTypes.MediaItem mediaItem, int reason) { }

    void initVideoPlayer()
    {
        _videoPlayerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        _currentMeshRenderer = GetComponent<MeshRenderer>();
        _defaultMaterials = _currentMeshRenderer.materials;
        _playerMaterials = new Material[_currentMeshRenderer.materials.Length];
        for (int i = 0; i < _currentMeshRenderer.materials.Length - 1; i++)
        {
            _playerMaterials[i] = _currentMeshRenderer.materials[i];
        }
        _playerMaterials[_currentMeshRenderer.materials.Length - 1] = _videoPlayerMaterial;

        _videoRenderTexture = new RenderTexture(720, 1280, 256);
        _videoPlayerMaterial.mainTexture = _videoRenderTexture;

        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
        _videoPlayer.targetCameraAlpha = 0.5F;
        _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
        _videoPlayer.targetTexture = _videoRenderTexture;
        _videoPlayer.loopPointReached += EndVideoCheckIOS;
    }

    void initExoPlayer()
    {
        _exoPlayer = new AndroidExoPlayer();
        _exoPlayer.addEventListener(this);

        _currentMeshRenderer = GetComponent<MeshRenderer>();
        _playerMaterials = new Material[_currentMeshRenderer.materials.Length];
        _videoPlayerMaterial = new Material(videoTextureShader);

        _defaultMaterials = _currentMeshRenderer.materials;
        for (int i = 0; i < _currentMeshRenderer.materials.Length - 1; i++)
        {
            _playerMaterials[i] = _currentMeshRenderer.materials[i];
        }
        _playerMaterials[_currentMeshRenderer.materials.Length - 1] = _videoPlayerMaterial;

        // videoMeshRenderer.materials = _playerMaterials;
        // videoMaterial = material;
        Texture2D videoTexture = (Texture2D)_exoPlayer?.initialize(gameObject);
        videoTexture.Apply();
        _videoPlayerMaterial.mainTexture = videoTexture;
    }

    [PunRPC]
    private void UpVolume()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    if(_exoPlayer.getVolume() < 1 ) _exoPlayer.setVolume(_exoPlayer.getVolume() + 0.1f);
    Debug.LogWarning("_exoPlayer Volume Up"+_exoPlayer.getVolume());

#elif UNITY_IOS || UNITY_EDITOR
        if (_videoPlayer.GetDirectAudioVolume(0) < 1) _videoPlayer.SetDirectAudioVolume(0, _videoPlayer.GetDirectAudioVolume(0) + 0.1f);

        Debug.LogWarning("_videoPlayer Volume Up" + _videoPlayer.GetDirectAudioVolume(0));
#endif
    }

    double _videoStartTime = 0;
    double _videoSyncTime = 0;

    private double GetCurrentTimeStamp()
    {
        DateTime dt = DateTime.Now;
        Debug.LogWarning("nowTime : " + ((DateTimeOffset)dt).ToUnixTimeSeconds());
        return ((DateTimeOffset)dt).ToUnixTimeSeconds();
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_exoPlayer == null) return;
        //if(_exoPlayer.getVolume()!= GameManager.Instance.Volume)_exoPlayer.setVolume(GameManager.Instance.Volume);

        if (_exoPlayerState == 3 && _exoPlayer.updateVideoTexture() && _videoPlayerMaterial.mainTextureScale != _exoPlayer.getVideoTextureScale() ) //&& (videoMaterial != null) 
        {
            _videoPlayerMaterial.mainTextureScale = _exoPlayer.getVideoTextureScale();
            Debug.Log("KAIKAI 123");
            return;
        }
#endif
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetVolume(0.1f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SetVolume(1f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Stop();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayNext();
        }

    }

    string _currentTime = "";

    IEnumerator FindYoutubeCurrentTime()
    {
        while (string.IsNullOrEmpty(_currentTime) || _currentTime.Equals("undefined") || _currentTime.Equals("1") || _currentTime.Equals("null"))
        {
            yield return new WaitForSeconds(0.1f);
            _currentTime = RequestYoutubeScript("document.getElementById('movie_player').getCurrentTime()");
            Debug.Log("While : " + _currentTime);
        }
    }

    private async void YoutubeSeekTo(double time)
    {
        await _webViewObject.WebView.WaitForNextPageLoadToFinish();
        RequestYoutubeScript("document.getElementById('movie_player').seekTo(" + time.ToString() + ")");
    }

    public void YoutubeVolume(float volume)
    {
        volume = volume * 100;
        string stringVolume = volume.ToString();
        RequestYoutubeScript("document.getElementById('movie_player').setVolume(" + stringVolume + ")");
    }

    public async void YoutubePlay(string url)
    {
        _webViewObject.WebView.LoadUrl(url);
        await _webViewObject.WebView.WaitForNextPageLoadToFinish();
        Debug.Log("A");
        StartCoroutine(EndTimeYoutube());

#if UNITY_ANDROID || UNITY_EDITOR
        _webViewObject.WebView.Click(new Vector2(0.25f, 0.25f));

        _webViewObject.Material.mainTextureOffset = new Vector2(0, 0.035f);
        _webViewObject.Material.mainTextureScale = new Vector2(0.99f, 0.56f);

        _currentMeshRenderer.materials = new Material[] { _currentMeshRenderer.material, _webViewObject.Material };
#endif

#if UNITY_IOS && !UNITY_EDITOR
            _currentMeshRenderer.materials = new Material[] { _currentMeshRenderer.material, _webViewObject.GetVideoMaterial() };
#endif
    }

    //* IOS MP4 Video End Event
    public void EndVideoCheckIOS(UnityEngine.Video.VideoPlayer vp)
    {
        Debug.Log("IOSVideoEnd");
#if UNITY_EDITOR || !UNITY_ANDROID
        if (_videoPlayer.isPlaying || !PhotonNetwork.IsMasterClient) return;
        PlayerActionManager palyer = FindObjectOfType<PlayerActionManager>();
        PlayNext();
#endif
    }

    // Youtube Video End find
    string _youtubeVideoLength = "";

    private IEnumerator EndTimeYoutube()
    {
        while (string.IsNullOrEmpty(_youtubeVideoLength) || _youtubeVideoLength.Equals("undefined") || _youtubeVideoLength.Equals("1") || _youtubeVideoLength.Equals("null"))
        {
            yield return new WaitForSeconds(0.1f);
            _youtubeVideoLength = RequestYoutubeScript("document.getElementById('movie_player').getDuration()");
        }

        RequestYoutubeScript("document.getElementById('movie_player').getCurrentTime()").Equals("");

        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (RequestYoutubeScript("document.getElementById('movie_player').getCurrentTime()") == _youtubeVideoLength)
            {
                Debug.Log("KAI Next");
                PlayNext();
                break;
            }
        }
    }

    public void UpdateUrl(string _url)
    {
        StartCoroutine(LoadUrl(_url));
    }

    private IEnumerator LoadUrl(string _url)
    {
        if (_exoPlayer != null) _exoPlayer.stop();
        if (_videoPlayer != null) _videoPlayer.Stop();

        RequestYoutubeScript("document.getElementById('movie_player').stopVideo()");

        if (_isYoutube)
        {
            if (!_isOpenPage)
            {
                _isOpenPage = true;
                yield return new WaitForSeconds(0.5f);

                YoutubePlay(_url);
            }
            else
            {
                // vuPlex 사용
                YoutubePlay(_url);
            }
        }
        else
        {
            Debug.Log(_currentMeshRenderer);
            Debug.Log(_playerMaterials);
            if (_currentMeshRenderer == null || _playerMaterials == null) yield break;
            if (_currentMeshRenderer.materials != _playerMaterials) _currentMeshRenderer.materials = _playerMaterials;
            _isOpenPage = false;
#if UNITY_EDITOR || !UNITY_ANDROID
            _videoPlayer.url = _url;
            _videoPlayer.Prepare();
            Debug.LogWarning("Load End");
#elif UNITY_ANDROID && !UNITY_EDITOR
            getPlayer().setMediaItem(ExoPlayerTypes.MediaItem.fromUri(_url));
            getPlayer().prepare();
            _exoPlayer?.onStart();
            Debug.LogWarning("_exoPlayer.Load End");
#endif   
        }

        _videoStartTime = GetCurrentTimeStamp();
        yield break;
    }

    string _responseText = "";
    private string RequestYoutubeScript(string script)
    {
        ExecuteJavaScript(script);
        return _responseText;
    }

    private async void ExecuteJavaScript(string script)
    {
        if (!_webViewObject) return;
        _responseText = await _webViewObject.WebView.ExecuteJavaScript(script);
    }

    private float startTime = 0f;

    void OnMouseDown()
    {
        if (SceneManager.GetActiveScene().name != "ChatRoom") return;
        startTime = Time.time;
    }

    void OnMouseUp()
    {
        if (SceneManager.GetActiveScene().name != "ChatRoom") return;
        float elapsedTime = Time.time - startTime;
        if (elapsedTime >= 1)
        {
            // Debug.Log("IsPlaying : " + IsPlaying());
            // if (IsPlaying()) StopPlayer();
            // else PlayPlayer();
        }
        else
        {
            JObject json = new JObject();
            json["cmd"] = "OnTapDisplay";
            RNMessenger.SendJson(json);
        }
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        StandaloneWebView.TerminateBrowserProcess();
#elif UNITY_EDITOR || !UNITY_ANDROID
        _videoPlayer.loopPointReached -= EndVideoCheckIOS;
#elif UNITY_ANDROID && !UNITY_EDITOR
        _exoPlayer.stop();
        _exoPlayer?.onDestroy();
#endif
    }

    public SimpleExoPlayer getPlayer()
    {
        return _exoPlayer;
    }

    //================================= 밍글플레이어 컨트롤러 ===================================
    // 밍글 플레이어 다음 영상 재생
    public void PlayNext()
    {
        _photonView.RPC("SetNextVideo", RpcTarget.All);
    }

    // 밍글 플레이어 재생
    public void Play()
    {
        _photonView.RPC("PlayPlayer", RpcTarget.All);
    }

    // 밍글 플레이어 정지
    public void Stop()
    {
        _photonView.RPC("StopPlayer", RpcTarget.All);
    }

    // 밍글 플레이어 볼륨 조절
    public void SetVolume(float volume)
    {
        if (_isYoutube) YoutubeVolume(volume);
        else
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    _exoPlayer.setVolume(volume);
    
#elif UNITY_IOS || UNITY_EDITOR
            _videoPlayer.SetDirectAudioVolume(0, volume);
#endif
        }
    }

    // 밍글 플레이어 재생 중 확인
    public bool IsPlaying()
    {
        if (_isYoutube)
        {
            if (RequestYoutubeScript("document.getElementById('movie_player').getPlayerState()") == "1") return true;
            else return false;
        }
        else
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            return _videoPlayer.isPlaying;
#elif UNITY_ANDROID && !UNITY_EDITOR
        return _exoPlayer.isPlaying();
#endif
        }
    }
    //==============================================================================================================

    [PunRPC]
    public void StopPlayer()
    {
        if (_isYoutube)
        {
            RequestYoutubeScript("document.getElementById('movie_player').pauseVideo()");
        }
        else
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            _videoPlayer.Pause();
#elif UNITY_ANDROID && !UNITY_EDITOR
        _exoPlayer?.pause();
#endif
        }
    }

    [PunRPC]
    public void PlayPlayer()
    {
        if (_isYoutube)
        {
            RequestYoutubeScript("document.getElementById('movie_player').playVideo()");
        }
        else
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            _videoPlayer.Play();
#elif UNITY_ANDROID && !UNITY_EDITOR
        Debug.LogWarning("_exoPlayer.Play()");
        getPlayer().setPlayWhenReady(true);
#endif
        }
    }

    //======================================== Start Video Index & Video Time <Sync> 맞추기 =====================================
    //* M-1 마스터 실행 : Current Video Index 넘겨주기 
    [PunRPC]
    private void RequestVideo(PhotonMessageInfo info)
    {
        //마스터 실행
        //현재 실행 중인 비디오 index?

        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!objHashtable.ContainsKey(VIDEOLIST) && !objHashtable.ContainsKey(VIDEOINDEX)) return;

        string[] VideoList = (string[])objHashtable[VIDEOLIST];
        int currentVideoIndex = (int)objHashtable[VIDEOINDEX];
        if (VideoList != null)
        {
            //* S-2 : 요청한 사람 실행 : VideoListPlay .
            _photonView.RPC("VideoListPlay", info.Sender, currentVideoIndex);
        }
        else Debug.Log("VideoList == Null");
    }

    //* M-3 마스터 실행 : VideoTime 넘겨주기
    [PunRPC]
    private IEnumerator RequestVideoTime(bool MasterPlatformIOS, PhotonMessageInfo info)
    {
        if (!IsPlaying()) yield break;

        double time = 0;

        // 타임스탬프 사용 
        _videoSyncTime = GetCurrentTimeStamp() - _videoStartTime;
        time = _videoSyncTime;
        yield return new WaitForSeconds(0.1f);

        // 커런트 타임 사용
        //if (_isYoutube)
        //{
        //    yield return StartCoroutine(FindYoutubeCurrentTime());
        //    //time = double.Parse(_currentTime);
        //    time = _videoSyncTime;
        //}
        //else
        //{
        //    if (Application.platform == RuntimePlatform.Android)
        //    {
        //        time = ValueConverter(IOSversion, MasterPlatformIOS, (double)GetAndroidVideoTime());
        //        Debug.Log("KAI time : " + time);
        //    }
        //    else if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WSAPlayerARM)
        //    {
        //        time = ValueConverter(IOSversion, MasterPlatformIOS, (double)GetIosVideoTime());
        //    }
        //}

        //* S-4 요청한사람 실행 : SyncVideoTime
        _photonView.RPC("SyncVideoTime", info.Sender, IsPlaying(), time);
        //_currentTime = "";
    }

    //* IOS(double) / Android(long) 들어온 사람 & 마스터 <IOS or Android> 판별 후 시간 변환 후 싱크타임 리턴
    //private double ValueConverter(bool senderPlatformIOS, bool MasterPlatformIOS, double input)
    //{
    //    if (senderPlatformIOS && !MasterPlatformIOS)
    //    {
    //        input = input * 1000;
    //    }
    //    else if (!senderPlatformIOS && MasterPlatformIOS)
    //    {
    //        input = input / 1000;
    //    }
    //    return input;
    //}

    [PunRPC]
    private IEnumerator SyncVideoTime(bool isPlaying, double time)
    {
        if (_isYoutube)
        {
            YoutubeSeekTo(time);
        }
        else
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            //! 비디오 싱크 맞추는 동안 화면 검은색 처리.
            videoMeshRenderer.materials[_playerMaterials.Length - 1].SetColor("_BaseColor", Color.black);
            long waitframe = 5;
            while (!_videoPlayer.isPrepared || _videoPlayer.frame < waitframe)
            {
                yield return new WaitUntil(() => _videoPlayer.isPrepared);
            }
            StartCoroutine(SetIosVideoTime(time));
#endif
            // 안드로이드 싱크의 seekTo 타입이 달라서 * 1000을 해줌.
#if UNITY_ANDROID && !UNITY_EDITOR
            while (getPlayer().isLoading())
            {
                yield return new WaitUntil(() => !getPlayer().isLoading());
            }
            SetAndroidVideoTime(((long)time*1000));
#endif
            //* 마스터쪽 비디오가 멈춰 있으면 Time 싱크 까지만 맞추고 비디오 멈춰놓기.
            if (!isPlaying) Stop();

        }
        yield break;
    }

    //* MP4 싱크 맞추는 동안 소리 0으로 설정.
    private void MP4WhileSyncingVolumeZero()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    _exoPlayer.setVolume(0);
#elif UNITY_IOS || UNITY_EDITOR
        _videoPlayer.SetDirectAudioVolume(0, 0);
#endif
    }
    //==============================================================================================================

    //================================================ GET SET MP4 VIDEO TIME ==========================================
    //private long GetAndroidVideoTime()
    //{
    //    Debug.Log("GetAndroidVideoTime" + getPlayer().getCurrentPosition());
    //    return getPlayer().getCurrentPosition();
    //}

    private void SetAndroidVideoTime(long _time)
    {
        getPlayer().seekTo(_time);
    }

    //private double GetIosVideoTime()
    //{
    //    Debug.Log("GetIosVideoTime" + _videoPlayer.time);
    //    return _videoPlayer.time;
    //}

    private IEnumerator SetIosVideoTime(double _time)
    {
        _videoPlayer.time = _time + 1;
        while (_videoPlayer.frame == 5)
        {
            yield return new WaitUntil(() => _videoPlayer.frame != 5);
        }

        //! 비디오 싱크 다 맞추면 화면 보이기
        videoMeshRenderer.materials[_playerMaterials.Length - 1].SetColor("_BaseColor", Color.white);
        PlayPlayer();
    }
    //=========================================================================================================

    //* 현재 비디오 업로드 후 비디오 플레이 RPC
    [PunRPC]
    void VideoListPlay(int currentVideoNum)
    {
        string[] VideoList = (string[])PhotonNetwork.CurrentRoom.CustomProperties[VIDEOLIST];

        if (VideoList[currentVideoNum].Contains("youtube"))
        {
            _isYoutube = true;
        }
        else _isYoutube = false;

        UpdateUrl(VideoList[currentVideoNum]);

        if (!_isYoutube) PlayPlayer();

    }

    //* 현재 비디오 업로드 후 비디오 <모두에게> 플레이
    public void RpcTargetAllVideoPlay()
    {
        int currentVideoIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties[VIDEOINDEX];
        string[] VideoList = (string[])PhotonNetwork.CurrentRoom.CustomProperties[VIDEOLIST];
        Debug.LogWarning("VideoList[" + currentVideoIndex + "] : " + VideoList[currentVideoIndex]);
        if (VideoList != null)
        {
            _photonView.RPC("VideoListPlay", RpcTarget.All, currentVideoIndex);
        }
    }

    //* 다음 비디오로 넘기기 RPC
    [PunRPC]
    private void SetNextVideo()
    {
        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
        string[] VideoList = (string[])objHashtable[VIDEOLIST];
        int currentVideoNum = (int)objHashtable[VIDEOINDEX];

        if (currentVideoNum == VideoList.Length - 1)
        {
            currentVideoNum = 0;
        }
        else currentVideoNum = currentVideoNum + 1;

        if (PhotonNetwork.IsMasterClient && _photonView.IsMine)
        {
            UpdateHashVideoNum(currentVideoNum);
        }

        VideoListPlay(currentVideoNum);
    }

    //* 현재 실행중인 비디오 인덱스 저장.
    void UpdateHashVideoNum(int index)
    {
        ExitGames.Client.Photon.Hashtable updateIndex = new ExitGames.Client.Photon.Hashtable();
        updateIndex[VIDEOINDEX] = index;
        PhotonNetwork.CurrentRoom.SetCustomProperties(updateIndex);
    }

    //* 1개 비디오 추가
    public void AddVideo(string videoName)
    {
        int currentIndex = 0;
        Debug.Log("Save_VideoName" + videoName);

        List<string> videoArray = new List<string>();

        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;

        if (objHashtable.ContainsKey(VIDEOLIST))
        {
            if (objHashtable[VIDEOLIST] == null) Debug.LogWarning("objHashtable[VIDEOLIST] == null"); objHashtable[VIDEOINDEX] = currentIndex;
            foreach (var a in (string[])objHashtable[VIDEOLIST])
            {
                videoArray.Add(a);
            }
            videoArray.Add(videoName);
            objHashtable[VIDEOLIST] = videoArray.ToArray();

        }
        else
        {
            videoArray.Add(videoName);
            if (objHashtable[VIDEOLIST] == null) Debug.LogWarning("objHashtable[VIDEOLIST] == null"); objHashtable.Add(VIDEOINDEX, currentIndex);
            objHashtable.Add(VIDEOLIST, videoArray.ToArray());
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
    }

    //* 2개 이상 비디오 추가
    public void AddVideo(List<string> videos)
    {
        int currentIndex = 0;
        Debug.Log("add videos" + videos.Count);

        List<string> videoArray = new List<string>();

        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;

        if (objHashtable.ContainsKey(VIDEOLIST))
        {
            videoArray.AddRange((string[])objHashtable[VIDEOLIST]);
            videoArray.AddRange(videos);

            objHashtable[VIDEOLIST] = videoArray.ToArray();
            objHashtable[VIDEOINDEX] = currentIndex;
        }
        else
        {
            videoArray.AddRange(videos);

            objHashtable.Add(VIDEOINDEX, currentIndex);
            objHashtable.Add(VIDEOLIST, videoArray.ToArray());
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
    }

    //* 비디오 리스트 비우기
    public void ClearVideo()
    {
        List<string> videoArray = new List<string>();

        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;

        if (objHashtable.ContainsKey(VIDEOLIST)) objHashtable[VIDEOLIST] = videoArray.ToArray();
        else objHashtable.Add(VIDEOLIST, videoArray.ToArray());
        if (objHashtable.ContainsKey(VIDEOINDEX)) objHashtable[VIDEOINDEX] = 0;
        else objHashtable.Add(VIDEOINDEX, 0);

        PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
    }

    public void SetVideo(string videoName)
    {
        Debug.Log("Save_VideoName" + videoName);

        List<string> videoArray = new List<string>();

        videoArray.Add(videoName);

        ExitGames.Client.Photon.Hashtable objHashtable = PhotonNetwork.CurrentRoom.CustomProperties;
        if (objHashtable.ContainsKey(VIDEOLIST)) objHashtable[VIDEOLIST] = videoArray.ToArray();
        else objHashtable.Add(VIDEOLIST, videoArray.ToArray());
        if (objHashtable.ContainsKey(VIDEOINDEX)) objHashtable[VIDEOINDEX] = 0;
        else objHashtable.Add(VIDEOINDEX, 0);
        PhotonNetwork.CurrentRoom.SetCustomProperties(objHashtable);
    }
}
