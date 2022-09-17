
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
        //[UdonSynced] [FieldChangeCallback(nameof(Cont0))] private bool _cont0=true; //(同期変数)true:スライドのページ数総数をワールドに埋め込む。false:スライドのページ数総数を動画から読み取る
        [UdonSynced] [FieldChangeCallback(nameof(Cont1))] private int _cont1 = 0; //(同期変数)true:スライド番号
        public float  AllPagenum;　//スライドのページ数総数(Inspectorで設定する)
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(allpagenum))] private float _allpagenum;//(同期変数)動画から取得したスライドのページ総数
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Endpage))] private float _endpage;//(同期変数)スライドのページ総数
        private VRCUrl seturl; // seturl1 or  seturl2 or  seturl3のいずれかが入る
        public VRCUrl seturl1; //Inspectorで与えた再生する動画のURL
        public VRCUrl seturl2; //Inspectorで与えた再生する動画のURL
        public VRCUrl seturl3; //Inspectorで与えた再生する動画のURL
        public VRCUrl seturl4; //Inspectorで与えた再生する動画のURL
        public Text Allpage,Nowpage, Allpage_, Nowpage_; // Allpage:スライドページ総数を表示するテキストオブジェクト, Nowpage:現在のスライドのページ数を表示するテキストオブジェクト
        public VRCUrlInputField inputField; //再生する動画のURLを入力するテキストボックスオブジェクト
        public Text statusText,UrlcText; //statusText:現在の状態を示すテキストオブジェクト, seigyo0Text:スライドのページ数総数をワールドに埋め込む or スライドのページ数総数を動画から読み取るのどちらの設定になっているかを表示するテキストオブジェクト
        public VRCUnityVideoPlayer unityVideoPlayer; //動画プレイヤーオブジェクトを定義する。
        public float timeSpan = 1f;
        private float timeOffset = 0.1f; // (Page-1) * timeSpan + timeOffset の地点で動画を停止させる。そのためのparameter値を設定する。
        public int slidenum = 3;
        public string[] slidetitle; //スライドのタイトル



        private Vector3 headP; //アバターheadの座標を格納する変数
        private Quaternion headR, rotOBJ; //headP:アバターheadのオイラー角, rotOBJ:オブジェクトの回転角
        public int RotX; //RotX:オブジェクト回転角(X軸)
        public float L;//オブジェクトとユーザーとの距離
        public GameObject Myobj; //オブジェクト自体を定義する
        public bool mode = false;




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

       // private bool Cont0   //(同期変数)true:スライドのページ数総数をワールドに埋め込む。false:スライドのページ数総数を動画から読み取る
        //{
          //  get => _cont0;
          //  set
          //  {
            //    _cont0 = value; 
             //   DisplaySeigyo0();
           // }
      //  }

        private int Cont1   //(同期変数)URLの切り替え
        {
            get => _cont1;
            set
            {
                _cont1 = value;
                Displayurlc();
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

        
        //****** (3-1)LT systemのコード *********


        private void Start()
        {
            seturl = seturl1;
            UrlcText.text = slidetitle[0];
           
        }



        //ReSyncを行う
        public void ReSync()
        {
            //Networking.SetOwner(Networking.LocalPlayer, gameObject); //ゲームオブジェクトの所有権をボタンをクリックしたユーザーに移す
            VRCUrl url;
            url = inputField.GetUrl();


            if (url.ToString() == "")
            {
                url = seturl;
            }

            if (url != null)
            {
                Debug.Log("OnURLChanged url: " + url.ToString());
                statusText.text = "Resync...";
                unityVideoPlayer.LoadURL(URL);
            
                //RequestSerialization();
            }
        }


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
                Endpage = allpagenum; // 動画の全再生時間をEndpageにアサインする。
                //Endpage = AllPagenum; //Inspectorで与えた動画の全再生時間をEndpageにアサインする。

            }
        }

        //変数Endpageの値を AllPagenum(外部から与えたページ数)またはallpagenum(動画の長さを自動で読み取った数値)のいずれかに切り替える。
        //public void Sengyo0()
        //{
          //  if (Networking.IsOwner(gameObject)) 
            //    {
             //   Cont0 = !Cont0;
              //  RequestSerialization();
                //Endpage = Cont0 ? AllPagenum : allpagenum;  //Cont0 =Trueならば AllPagenum(外部から与えたページ数)を使う、 Cont0=Falseならば allpagenum(動画の長さを自動で読み取った数値)を使う
              //  RequestSerialization();

            //}
        //}


        //表示するスライドの切り替えを行う。
        public void Urlchange()
        {
            if (Networking.IsOwner(gameObject))
            {
                Cont1++;
                RequestSerialization();





                switch (Cont1 % slidenum)
                {
                    case 0:
                        seturl = seturl1;
                        break;
                    case 1:
                        seturl = seturl2;
                        break;
                    case 2:
                        seturl = seturl3;
                        break;

                }
  
                RequestSerialization();

            }
        }



       // public void DisplaySeigyo0()
        //{
          //  if (Cont0 == true)
          //  {
            //    seigyo0Text.text = "手動";    // データ表示更新
          //  }
          //  else if(Cont0 == false)
            //{
              //  seigyo0Text.text = "自動";    // データ表示更新
           // }

        //}


        public void Displayurlc()
        {
            UrlcText.text = slidetitle[Cont1 % slidenum]; // データ表示更新


          //  switch (Cont1 % slidenum)
           // {
             //   case 0:
               //     UrlcText.text = slidetitle[0];    // データ表示更新
                //    break;
               // case 1:
                 //   UrlcText.text = slidetitle[1];    // データ表示更新
                 //   break;
                //case 2:
                  //  UrlcText.text = slidetitle[2];    // データ表示更新
                   // break;

            //}
   

        }


        // 同期変数の値をUIに表示する処理
        public void DisplayNowpage()
        {
            Nowpage.text = Page.ToString();    // データ表示更新
            Nowpage_.text = Nowpage.text;

        }


        // 同期変数の値をUIに表示する処理
        public void DisplayEndpage()
        {
            Allpage.text = Endpage.ToString();    // データ表示更新
            Allpage_.text = Allpage.text;
        }



        // 同期変数の値をUIに表示する処理
        public void DisplayAllpage()
        {
            Allpage.text = allpagenum.ToString();    // データ表示更新
            Allpage_.text = Allpage.text;
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



        //****** (3-2)補助スライド制御コード *********


        //アバターheadの座標,オイラー角を取得するメソッド
        public void zahyouget()
        {
            //初期位置の設定
            var player = Networking.LocalPlayer;

            Debug.Log("transform.position");
            if (player != null)
            {
                var headData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head); //自分自身の座標を取得する。
                headP = headData.position; //アバターheadの座標を返す。
                headR = headData.rotation; //アバターheadのオイラー角を返す。
            }
        }

        //ボタンをクリックすると、補助スライドを表示、非表示にする。
        public void Pressed()
        {


            if (Networking.IsOwner(gameObject))
            {
                mode = !mode;
                Myobj.SetActive(mode);
                //displayMode.text = mode.ToString();
            }
            else
            {
                statusText.text = "Owner: " + Networking.GetOwner(gameObject).displayName;
            }
        }

        //アバターに合わせ、補助スライドを移動させる
        void Update()
        {
            //初期位置の設定
            var player = Networking.LocalPlayer;
            var headData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head); //自分自身の座標を取得する。

            headP = headData.position; //アバターheadの座標を返す。
            headR = headData.rotation; //アバターheadのQuaternionを返す。

            Vector3 rotEuler = headR.eulerAngles; //アバターheadのQuaternionをオイラー角に変換する
            //rotEuler.x = RotX; //x軸オイラー角のみ、回転する角度を指定する。もしこの行がない場合、オブジェクトは常にアバターの視点方向に表示される。
            rotOBJ = Quaternion.Euler(rotEuler); //オイラー角をクォータニオ ンに戻し、プレイヤー座標とずらす距離を計算する
            Myobj.transform.position = headP + rotOBJ * new Vector3(0, 0, L); //アバターheadの位置を中心とし、z軸方向に0.5m移動、かつオイラー角(RotXで指定した数値,アバターの視点軸角度,アバターの視点軸角度)方向に回転する。
            Vector3 myObjloc = Myobj.transform.position;

            //座標表示
            Myobj.transform.rotation = headR;
            //displayP1.text = headP.ToString();　 //アバターheadの座標を表示する
            //displayP2.text = rotOBJ.ToString();　//アバターheadのオイラー角を表示
            //displayP3.text = myObjloc.ToString();　 //対象オブジェクトの座標を表示
            //displayP4.text = rotEuler.ToString();　//オブジェクトの回転量

        }


    }












}
