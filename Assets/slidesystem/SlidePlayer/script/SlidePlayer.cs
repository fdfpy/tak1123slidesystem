
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Components.Video;

namespace VRCLT
{
    [AddComponentMenu("SlidePlayer")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SlidePlayer : UdonSharpBehaviour
    {
        [UdonSynced] [FieldChangeCallback(nameof(URL))]
        VRCUrl _syncedURL;

        [UdonSynced] [FieldChangeCallback(nameof(Page))]
        private int _syncedPage;
        public int  AllPagenum;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(allpagenum))] private float _allpagenum;
        public VRCUrl seturl;
        public Text Allpage,Nowpage;


        private VRCUrl URL
        {
            get => _syncedURL;
            set
            {
                _syncedURL = value;
                Debug.Log("ChangeVideoURL: " + value);
                unityVideoPlayer.LoadURL(value);
            }
        }

        private int Page
        {
            get => _syncedPage;
            set
            {
                _syncedPage = value;
                ChangeVideoPosition();
            }
        }


        private float allpagenum
        {
            get => _allpagenum;
            set
            {
                _allpagenum = value; //valueキーワードが使用でき、これにアクセス元から渡された値が格納されています。
                DisplayAllpage();
            }
        }








        public VRCUrlInputField inputField;
        public Text statusText;

        public VRCUnityVideoPlayer unityVideoPlayer;

        public float timeSpan = 1f;
        private float timeOffset = 0.1f;

        public void OnTakeOwnershipClicked()
        {
            Debug.Log("Take ownership");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            statusText.text = "Changed owner";
            inputField.gameObject.SetActive(true);
        }

        public void OnURLChanged()
        {


            VRCUrl url;
            //url = seturl;
            url = inputField.GetUrl();


            if (url.ToString() == "") {
              url = seturl;
             }


            //Allpage.text = url.ToString();



            if (Networking.IsOwner(gameObject) && url != null)
            {
                Debug.Log("OnURLChanged url: " + url.ToString());
                statusText.text = "Loading...";
                Page = 1;
                URL = url;
                RequestSerialization();
            }
            else
            {
                statusText.text = "You must be the owner to set the URL ";
            }
        }

        public void OnNextSlideButtonClick()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("OnNextSlideButtonClick as owner");
                if (Page < AllPagenum)
                {
                    Page++;
                    RequestSerialization();
                }
            }
            else
            {
                statusText.text = "Owner: " + Networking.GetOwner(gameObject).displayName;
            }
        }

        public void OnPrevSlideButtonClick()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("OnPrevSlideButtonClick as owner");
                if (Page > 1)
                {
                    Page--;
                    RequestSerialization();
                }
            }
        }

        public void OnResetButtonClick()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("OnResetButtonClick as owner");
                Page = 1;
                RequestSerialization();
            }
        }


        private void ChangeVideoPosition()
        {
            Debug.Log("ChangeVideoPosition: " + Page);
            unityVideoPlayer.SetTime((Page-1) * timeSpan + timeOffset);
            Nowpage.text = Page.ToString();
        }

        public override void OnOwnershipTransferred()
        {
            if (!Networking.IsOwner(gameObject))
            {
                inputField.gameObject.SetActive(false);
            }
        }

        public override void OnVideoReady()
        {
            Debug.Log("OnVideoReady");
            ChangeVideoPosition();
            if (!Networking.IsOwner(gameObject))
            {
                statusText.text = "Video ready. Owner: " + Networking.GetOwner(gameObject).displayName;

               
            }
            else
            {
                statusText.text = "Video ready. click \"next\" on control panel to start presentation.";
               
                allpagenum = unityVideoPlayer.GetDuration(); //動画の全再生時間を取得する。
                Allpage.text = allpagenum.ToString();



            }
        }


        // 同期変数の値をUIに表示する処理
        public void DisplayAllpage()
        {
            Allpage.text = allpagenum.ToString();    // データ表示更新
        }

        public override void OnVideoError(VideoError videoError)
        {

            switch (videoError)
            {
                case VideoError.RateLimited:
                    statusText.text = "Rate limited, try again in a few seconds";
                    break;
                case VideoError.PlayerError:
                    statusText.text = "Video player error";
                    break;
                case VideoError.InvalidURL:
                    statusText.text = "Invalid URL";
                    break;
                case VideoError.AccessDenied:
                    statusText.text = "Video blocked, enable untrusted URLs";
                    break;
                default:
                    statusText.text = "Failed to load video";
                    break;
            }
        }
    }
}
