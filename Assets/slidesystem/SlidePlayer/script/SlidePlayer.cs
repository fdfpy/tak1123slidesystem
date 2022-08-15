
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Components.Video;


namespace VRCLT
{
    [AddComponentMenu("SlidePlayer")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SlidePlayer : UdonSharpBehaviour
    {

        //****** (1) 使用する変数を定義する *********

        [UdonSynced] [FieldChangeCallback(nameof(URL))] VRCUrl _syncedURL; //(同期変数)再生する動画のURL
        [UdonSynced] [FieldChangeCallback(nameof(Page))]private int _syncedPage; //(同期変数)スライドのページ番号
        [UdonSynced] [FieldChangeCallback(nameof(Cont0))] private bool _cont0=true; //(同期変数)true:スライドのページ数総数をワールドに埋め込む。false:スライドのページ数総数を動画から読み取る
        public float  AllPagenum;　//スライドのページ数総数(Inspectorで設定する)
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(allpagenum))] private float _allpagenum;//(同期変数)動画から取得したスライドのページ総数
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Endpage))] private float _endpage;//(同期変数)スライドのページ総数
        public VRCUrl seturl; //Inspectorで与えた再生する動画のURL
        public Text Allpage,Nowpage; // Allpage:スライドページ総数を表示するテキストオブジェクト, Nowpage:現在のスライドのページ数を表示するテキストオブジェクト
        public VRCUrlInputField inputField; //再生する動画のURLを入力するテキストボックスオブジェクト
        public Text statusText,seigyo0Text; //statusText:現在の状態を示すテキストオブジェクト, seigyo0Text:スライドのページ数総数をワールドに埋め込む or スライドのページ数総数を動画から読み取るのどちらの設定になっているかを表示するテキストオブジェクト
        public VRCUnityVideoPlayer unityVideoPlayer; //動画プレイヤーオブジェクトを定義する。
        public float timeSpan = 1f;
        private float timeOffset = 0.1f; // (Page-1) * timeSpan + timeOffset の地点で動画を停止させる。そのためのparameter値を設定する。


        //****** (2) 同期変数にプロパティーを定義する。 *********

        private VRCUrl URL //(同期変数)再生する動画のURL
        {
            get => _syncedURL;
            set
            {
                _syncedURL = value;
                Debug.Log("ChangeVideoURL: " + value);
                unityVideoPlayer.LoadURL(value);
            }
        }

        private int Page //(同期変数)スライドのページ番号
        {
            get => _syncedPage;
            set
            {
                _syncedPage = value;
                ChangeVideoPosition();
                DisplayNowpage();
            }
        }


        private float allpagenum  //(同期変数)動画から取得したスライドのページ総数
        {
            get => _allpagenum;
            set
            {
                _allpagenum = value; 
                DisplayAllpage();
            }
        }

        private bool Cont0   //(同期変数)true:スライドのページ数総数をワールドに埋め込む。false:スライドのページ数総数を動画から読み取る
        {
            get => _cont0;
            set
            {
                _cont0 = value; 
                DisplaySeigyo0();
            }
        }


        private float Endpage //(同期変数)スライドのページ総数
        {
            get => _endpage;
            set
            {
                _endpage = value; //valueキーワードが使用でき、これにアクセス元から渡された値が格納されています。
                DisplayEndpage();
            }
        }


        //****** (3) 各種メソッドを定義する。 *********







        //オブジェクトの操作権限を取得する。
        public void OnTakeOwnershipClicked()
        {
            Debug.Log("Take ownership");
            Networking.SetOwner(Networking.LocalPlayer, gameObject); //ゲームオブジェクトの所有権をボタンをクリックしたユーザーに移す
            statusText.text = "Changed owner";
            inputField.gameObject.SetActive(true);
        }

        //スライドの読み込みを行う関数
        public void OnURLChanged()
        {

            VRCUrl url;
            url = inputField.GetUrl();


            if (url.ToString() == "") {
              url = seturl;
             }

            if (Networking.IsOwner(gameObject) && url != null)
            {
                Debug.Log("OnURLChanged url: " + url.ToString());
                statusText.text = "Loading...";
                //Page = 1;
                URL = url;
                RequestSerialization();
            }
            else
            {
                statusText.text = "You must be the owner to set the URL ";
            }
        }

        //スライドのページを1ページ進める関数。ただし、スライドのページが最終ページまで到達した場合はPage++の動作を停止する。
        public void OnNextSlideButtonClick()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("OnNextSlideButtonClick as owner");

      

                if (Page < Endpage)
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

        //スライドのページを1ページ戻す関数
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
            else
            {
                statusText.text = "Owner: " + Networking.GetOwner(gameObject).displayName;
            }

        }

        //スライドのページを初めに戻す関数
        public void OnResetButtonClick()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("OnResetButtonClick as owner");
                Page = 1;
                RequestSerialization();
            }
            else
            {
                statusText.text = "Owner: " + Networking.GetOwner(gameObject).displayName;
            }


        }


        //再生していている動画に対し、設定した時間に移動し、動画を停止する関数
        private void ChangeVideoPosition()
        {
            Debug.Log("ChangeVideoPosition: " + Page);
            unityVideoPlayer.SetTime((Page-1) * timeSpan + timeOffset);
            //Nowpage.text = Page.ToString();
        }

        public override void OnOwnershipTransferred()
        {
            if (!Networking.IsOwner(gameObject))
            {
                inputField.gameObject.SetActive(false);
            }
        }

        //動画の読み込みを終了したら実行する関数
        public override void OnVideoReady()
        {
            Debug.Log("OnVideoReady");
            Page = 1;

            //ChangeVideoPosition();
            if (!Networking.IsOwner(gameObject))
            {
                statusText.text = "Video ready. Owner: " + Networking.GetOwner(gameObject).displayName;

               
            }
            else
            {
                statusText.text = "Video ready. click \"next\" on control panel to start presentation.";
               
                allpagenum = unityVideoPlayer.GetDuration(); //動画の全再生時間を取得する。
                Endpage = AllPagenum; //Inspectorで与えた動画の全再生時間をEndpageにアサインする。

            }
        }

        //変数Endpageの値を AllPagenum(外部から与えたページ数)またはallpagenum(動画の長さを自動で読み取った数値)のいずれかに切り替える。
        public void Sengyo0()
        {
            if (Networking.IsOwner(gameObject)) 
                {
                Cont0 = !Cont0;
                RequestSerialization();
                Endpage = Cont0 ? AllPagenum : allpagenum;  //Cont0 =Trueならば AllPagenum(外部から与えたページ数)を使う、 Cont0=Falseならば allpagenum(動画の長さを自動で読み取った数値)を使う
                RequestSerialization();

            }
        }


        public void DisplaySeigyo0()
        {
            if (Cont0 == true)
            {
                seigyo0Text.text = "手動入力";    // データ表示更新
            }
            else if(Cont0 == false)
            {
                seigyo0Text.text = "自動入力";    // データ表示更新
            }



        }

        // 同期変数の値をUIに表示する処理
        public void DisplayNowpage()
        {
            Nowpage.text = Page.ToString();    // データ表示更新
        }


        // 同期変数の値をUIに表示する処理
        public void DisplayEndpage()
        {
            Allpage.text = Endpage.ToString();    // データ表示更新
        }



        // 同期変数の値をUIに表示する処理
        public void DisplayAllpage()
        {
            Allpage.text = allpagenum.ToString();    // データ表示更新
        }

        //動画再生時に発生したエラーを表示させる。
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
