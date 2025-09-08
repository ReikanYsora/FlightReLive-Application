using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace FlightReLive.UI.Splashscrren
{
    public class SplashscreenManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private TMP_Text _companyNameText;
        [SerializeField] private TMP_Text _versionText;
        [SerializeField] private float _animationSpeed;
        [SerializeField] private Image _animatedLine;
        [SerializeField] private TMP_Text _animatedText;
        [SerializeField] private Color _animationColorStart;
        [SerializeField] private Color _animationColorEnd;
        [SerializeField] private string _nextSceneName = "Flight ReLive";
        [SerializeField] private Canvas _splashCanvas;
        [SerializeField] private Camera _camera;
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            _companyNameText.text = Application.companyName + " - 2025";
            _versionText.text = Application.version;
        }

        private void Start()
        {
            StartCoroutine(InitializeSplash());
        }
        #endregion

        #region METHODS
        private IEnumerator InitializeSplash()
        {
            if (_splashCanvas != null)
            {
                _splashCanvas.enabled = false;
            }

            if (_camera != null)
            {
                _camera.Render();
            }

            yield return null;

            if (_splashCanvas != null)
            {
                _splashCanvas.enabled = true;
            }

            AnimateSplashColors();
            yield return StartCoroutine(LoadNextSceneAsync());
        }

        private void AnimateSplashColors()
        {
            _animatedLine.color = _animationColorStart;
            _animatedText.color = _animationColorStart;

            _animatedLine.DOColor(_animationColorEnd, _animationSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            _animatedText.DOColor(_animationColorEnd, _animationSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private IEnumerator LoadNextSceneAsync()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_nextSceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                if (asyncLoad.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(3f);
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }
        }
        #endregion
    }
}
