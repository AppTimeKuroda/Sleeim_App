﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kaimin.Managers;
using System;
using MiniJSON;

/// <summary>
/// デバイス設定画面管理クラス
/// </summary>
public class DeviceSettingViewController : ViewControllerBase {

    [SerializeField] Text suppressionStartTimeText = null;

    /// <summary>
    /// 一時保存されたデバイス設定
    /// </summary>
    public static DeviceSetting TempDeviceSetting;

    /// <summary>
    /// シーンタグ
    /// </summary>
    /// <value>デバイス設定タグ</value>
    public override SceneTransitionManager.LoadScene SceneTag {
        get {
            return SceneTransitionManager.LoadScene.DeviceSetting;
        }
    }

    /// <summary>
    /// シーン開始イベントハンドラ
    /// </summary>
    protected override void Start() {
        base.Start();
        if (TempDeviceSetting == null) {
            TempDeviceSetting = UserDataManager.Setting.DeviceSettingData.Load();
        }

        if (TempDeviceSetting != null)
        {
            suppressionStartTimeText.text = (int)TempDeviceSetting.SuppressionStartTime + "分";
        }
    }

    /// <summary>
    /// 戻るボタン押下イベントハンドラ
    /// </summary>
    public void OnReturnButtonTap() {
        FlushTempDeviceSetting();
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Setting);
    }

    /// <summary>
    /// 保存ボタン押下イベントハンドラ
    /// </summary>
    public void OnSaveButtonTap() {
        StartCoroutine(ChangeDeviceSettingCoroutine());
    }

    /// <summary>
    /// 動作モードボタン押下イベントハンドラ
    /// </summary>
    public void OnActionButtonButtonTap() {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.ActionMode);
    }

    /// <summary>
    /// いびき感度ボタン押下イベントハンドラ
    /// </summary>
    public void OnSnoreSensitivityButtonTap() {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.SnoreSensitivity);
    }

    /// <summary>
    /// 抑制強度ボタン押下イベントハンドラ
    /// </summary>
    public void OnSuppressionStrengthButtonTap() {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.SuppressionStrength);
    }

    /// <summary>
    /// 抑制動作最大継続時間ボタン押下イベントハンドラ
    /// </summary>
    public void OnSuppressionOperationMaxTimeButtonTap() {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.SuppressionOperationMaxTime);
    }

    /// <summary>
    /// 抑制開始時間ボタン押下イベントハンドラ
    /// </summary>
    public void OnSuppressionStartTimeButtonTap()
    {
        //ピッカーを表示して抑制開始時間を設定させる
        string title = "抑制開始時間設定";
        string unit = "分";
        float maxValue = (float)SuppressionStartTime.Max;
        float minValue = (float)SuppressionStartTime.Min;
        float stepValue = 1;
        float currentValue = (float)TempDeviceSetting.SuppressionStartTime;
        var vs = new SelectValueDialogParamSet(
            SelectValueDialogParamSet.DISPLAY_TYPE.Numeric,
            title,
            unit,
            maxValue,
            minValue,
            stepValue,
            currentValue);
        SelectValueDialog.Show(vs, (SelectValueDialog.ButtonItem status, float value, GameObject dialog) => {
            if (status == SelectValueDialog.ButtonItem.OK)
            {
                //結果をテキストに反映
                suppressionStartTimeText.text = value.ToString("0") + unit;
                
                //アプリ内保存
                TempDeviceSetting.SuppressionStartTime = (SuppressionStartTime)value;
                SaveDeviceSetting();
            }
            else
            {
                //なにもしない
            }
        });
    }

    /// <summary>
    /// デバイス設定を変更するコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChangeDeviceSettingCoroutine() {
        Debug.Log("DeviceSetting: start ChangeDeviceSettingCoroutine");
        bool isSuccess = false;
        yield return StartCoroutine(SendCommandToDeviceCoroutine(
            DeviceSetting.CommandCode,
            (bool b) => isSuccess = b));
        if (isSuccess) {
            SaveDeviceSetting();
        } else {
            yield return StartCoroutine(ShowMessageDialogCoroutine("設定変更に失敗しました。"));
        }
    }

    /// <summary>
    /// 一時保存したデバイス設定をアプリに保存する
    /// </summary>
    private void SaveDeviceSetting() {
        UserDataManager.Setting.DeviceSettingData.Save(TempDeviceSetting);
    }

    /// <summary>
    /// 一時保存されたデバイス設定を破棄する
    /// </summary>
    private void FlushTempDeviceSetting() {
        TempDeviceSetting = null;
    }

    /// <summary>
    /// コマンド通信を行うコルーチン (デバイス設定変更、バイブレーション確認、バイブレーション停止など)
    /// </summary>
    /// <param name="commandCode">CommandCode(デバイス設定変更)、CommandCodeVibrationConfirm(バイブレーション確認)、CommandCodeVibrationStop(バイブレーション停止)</param>
    /// <param name="callback">デバイス設定変更が成功したかを返す</param>
    /// <returns></returns>
    protected IEnumerator SendCommandToDeviceCoroutine(byte commandCode, Action<bool> callback) {
        String coroutineName = "ChangeDeviceSetting"; //Default
        String coroutineMessage = "同期中"; //Default
        if (commandCode == DeviceSetting.CommandCodeVibrationConfirm)
        {
            coroutineName = "VibrationConfirm";
            coroutineMessage = "バイブレーション確認中";
        }
        else if (commandCode == DeviceSetting.CommandCodeVibrationStop)
        {
            coroutineName = "VibrationStop";
            coroutineMessage = "バイブレーション停止中";
        }

        Debug.Log("DeviceSetting: start Send" + coroutineName + "Coroutine, CommandCode: " + commandCode);
        if (!BluetoothManager.Instance.IsBluetoothEnabled()) {
            bool isBluetoothEnabled = false;
            yield return StartCoroutine(
                RequestBluetoothPermissionCoroutine(
                    (bool b) => isBluetoothEnabled = b));
            if (!isBluetoothEnabled) yield break;
        }
        Debug.Log("DeviceSetting: Bluetooth OK");

        if (UserDataManager.State.isDoneDevicePareing()) {
            Debug.Log("DeviceSetting: paring OK");
            if (!UserDataManager.State.isConnectingDevice()) {
                Debug.Log("DeviceSetting: start connection");
                string deviceName = UserDataManager.Device.GetPareringDeviceName();
                string deviceAdress = UserDataManager.Device.GetPareringBLEAdress();
                bool isConnected = false;
                ShowConnectingDialog(deviceName);
                yield return StartCoroutine(
                    BluetoothManager.Instance.ConnectDeviceCoroutine(
                        deviceName,
                        deviceAdress,
                        (bool b) => isConnected = b));
                Debug.Log ("Connecting_Result:" + isConnected);
                CloseConnectingDialog();
                if (!isConnected) {
                    yield return StartCoroutine(
                        ShowConnectionFailedDialogCoroutine(deviceName));
                    yield break;
                }
            }
        } else {
            yield return StartCoroutine(ShowParingErrorDialogCoroutine());
            yield break;
        }
        Debug.Log("DeviceSetting: connected");

        UpdateDialog.Show(coroutineMessage);
        bool? isCommunicationSuccess = null;
        BluetoothManager.Instance.SendCommandToDevice(
            commandCode,
            TempDeviceSetting,
            (string data) => {
                //エラー時
                Debug.Log (coroutineName + " error:" + data);
                isCommunicationSuccess = false;
            },
            (bool success) => {
                //コマンド書き込み結果
                Debug.Log (coroutineName + " write:" + success);
                if (!success) isCommunicationSuccess = false;
            },
            (string data) => {
                //応答結果
                Debug.Log (coroutineName + " response:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                bool response = Convert.ToBoolean(json["KEY2"]);
                isCommunicationSuccess = response;
            });
        yield return new WaitUntil(() => isCommunicationSuccess != null);
        UpdateDialog.Dismiss();
        callback((bool)isCommunicationSuccess);
    }

    /// <summary>
    /// Bluetooth使用許可をユーザーに求める
    /// </summary>
    /// <returns></returns>
    private IEnumerator RequestBluetoothPermissionCoroutine(Action<bool> callback) {
        bool isBluetoothEnabled = false;
        bool isSettingOn = false;
        yield return StartCoroutine(ShowDialogCoroutineToChangeSetting(
            (bool _isSettingOn) => isSettingOn = _isSettingOn));
        if (isSettingOn) {
            NativeManager.Instance.BluetoothRequest();
#if UNITY_ANDROID
            yield return new WaitUntil(() => NativeManager.Instance.PermissionCode > 0);
            isBluetoothEnabled = NativeManager.Instance.PermissionCode == 1;
#elif UNITY_IOS
            // iOSの場合、ユーザーの選択が受け取れなかったため、
            // 拒否された前提で進める
            isBluetoothEnabled = false;
#endif
        } else {
            isBluetoothEnabled = false;
        }
        callback(isBluetoothEnabled);
        yield return null;
    }

    /// <summary>
    /// 設定変更のためのダイアログを表示する
    /// </summary>
    /// <param name="callback">Bluetooth設定を行うかどうかを返す</param>
    /// <returns></returns>
    private IEnumerator ShowDialogCoroutineToChangeSetting(Action<bool> callback) {
        bool? isSettingOn = null;
        MessageDialog.Show(
            "<size=30>Bluetoothがオフになっています。\n"
            + "Sleeimと接続できるようにするには、\n"
            + "Bluetoothをオンにしてください。</size>",
            useOK: true,
            useCancel: true,
            onOK: () => isSettingOn = true,
            onCancel: () => isSettingOn = false,
            positiveItemName: "設定",
            negativeItemName: "キャンセル");
        yield return new WaitUntil(() => isSettingOn != null);
        callback((bool)isSettingOn);
        yield return null;
    }

    /// <summary>
    /// デバイスに接続中ダイアログを表示する
    /// </summary>
    /// <param name="deviceName">デバイス名</param>
    private void ShowConnectingDialog(string deviceName) {
        UpdateDialogAddButton.Show(
            deviceName + "に接続しています。",
            useOK: false,
            useCancel: true,
            onOK: null,
            onCancel: () => {
                BluetoothManager.Instance.Disconnect();
            },
            positiveItemName: "OK",
            negativeItemName: "キャンセル");
    }

    /// <summary>
    /// 接続中ダイアログを閉じる
    /// </summary>
    private void CloseConnectingDialog() {
        UpdateDialogAddButton.Dismiss();
    }

    /// <summary>
    /// 接続失敗ダイアログを表示する
    /// </summary>
    /// <param name="deviceName">デバイス名</param>
    /// <returns></returns>
    private IEnumerator ShowConnectionFailedDialogCoroutine(string deviceName) {
        bool isOK = false;
        MessageDialog.Show(
            "<size=32>" + deviceName + "と接続できませんでした。</size>",
            useOK: true,
            useCancel: false,
            onOK: () => isOK = true);
        yield return new WaitUntil (() => isOK);
    }

    /// <summary>
    /// ペアリングエラーダイアログを表示する
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowParingErrorDialogCoroutine() {
        bool isOK = false;
        MessageDialog.Show (
            "本体機器とのペアリングが完了していないため、処理を行えません。\n"
            + "本体機器とのペアリングを行ってください。",
            useOK: true,
            useCancel: false,
            onOK: () => isOK = true,
            onCancel: null,
            positiveItemName: "OK",
            negativeItemName: "キャンセル");
        yield return new WaitUntil (() => isOK);
    }

    /// <summary>
    /// デバイス設定変更失敗ダイアログを表示する
    /// </summary>
    /// <returns></returns>
    protected IEnumerator ShowMessageDialogCoroutine(String message) {
        bool isOk = false;
        MessageDialog.Show (
            message,
            useOK: true,
            useCancel: false,
            onOK: () => isOk = true);
        yield return new WaitUntil (() => isOk);
    }
}
