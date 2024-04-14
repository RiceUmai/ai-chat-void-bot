using AAA.OpenAI;
using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using UniBudouX;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;

public class ChatBotController : MonoBehaviour
{
    [SerializeField]
    private ChatBotView view;
    private ChatBotModel model = new ChatBotModel();

    private void Awake()
    {
        model.ChatGPTConnection = new ChatGPTConnection(view.OpenAPIKey.APIKey);
        BindView();
    }

    private void BindView()
    {
        view.SendMessageBtn
            .OnClickAsObservable()
            .AsObservable()
            .ThrottleFirst(TimeSpan.FromMilliseconds(2000))
            .Subscribe(_ =>
            {
                OnSendMessageToChatGPTEvent().Forget();
            }).AddTo(this);

        view.OpenLicenseBtn
            .OnClickAsObservable()
            .AsObservable()
            .ThrottleFirst(TimeSpan.FromMilliseconds(2000))
            .Subscribe(_ =>
            {
                OnOpenLicenseButton();
            }).AddTo(this);
    }

    private async UniTask OnSendMessageToChatGPTEvent()
    {
        try
        {
            view.SendMessageBtn.interactable = false;

            ChatGPTResponseModel response = await model.ChatGPTConnection.RequestAsync(view.MessageInputField.text);
            string responseMessage = response.choices[0].message.content;
            List<string> segmentedTexts = Parser.Parse(responseMessage);
            List<string> segmentedTexts100Char = SegmentTextInto100Char(segmentedTexts);

            view.ResponseMessageText.text = string.Empty;
            string currentText = string.Join(Environment.NewLine, segmentedTexts100Char);

            view.ResponseMessageText.DOText(currentText, currentText.Length * 0.1f).SetEase(Ease.Linear);

            List<AudioClip> audioClipList = await view.StyleBertVITS2APIManager.SendVoiceRequest(segmentedTexts100Char);
            await view.AudioManager.PlayAudioClip(audioClipList, model.Cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            if (view.SendMessageBtn != null)
            {
                view.SendMessageBtn.interactable = true;
            }
        }
    }

    private List<string> SegmentTextInto100Char(List<string> phrases)
    {
        List<string> segmentedTexts = new List<string>();
        string currentText = "";

        foreach (var phrase in phrases)
        {
            if ((currentText.Length + phrase.Length) <= 100)
            {
                currentText += phrase;
            }
            else
            {
                segmentedTexts.Add(currentText);
                currentText = phrase;
            }
        }

        if (!string.IsNullOrEmpty(currentText))
        {
            segmentedTexts.Add(currentText);
        }

        return segmentedTexts;
    }

    private void OnOpenLicenseButton()
    {
        Application.OpenURL("https://tyc.rei-yumesaki.net/about/project/");
    }

    private void OnDestroy()
    {
        if (model.Cts != null)
        {
            model.Cts.Cancel();
            model.Cts.Dispose();
        }
    }
}
