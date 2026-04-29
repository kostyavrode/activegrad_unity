using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ActiveGrad.MiniGames
{
    /// <summary>
    /// Компонент бонусного ползунка для экрана окончания игры.
    /// Ползунок бегает 1 секунду, затем останавливается.
    /// Позиция остановки зависит от уровня профессии.
    /// Ближе к центру = больше бонус к очкам.
    /// </summary>
    public class BonusSliderComponent : MonoBehaviour
    {
        [SerializeField] private RectTransform _sliderTrack;
        [SerializeField] private RectTransform _indicator;
        [SerializeField] private TMP_Text _leftLabel;
        [SerializeField] private TMP_Text _rightLabel;
        [SerializeField] private GameObject _finishButtonContainer;

        public RectTransform SliderTrack => _sliderTrack;
        public RectTransform Indicator => _indicator;
        public TMP_Text LeftLabel => _leftLabel;
        public TMP_Text RightLabel => _rightLabel;
        public GameObject FinishButtonContainer => _finishButtonContainer;

        private float _sliderDuration = 1f;

        public void Setup(RectTransform sliderTrack, RectTransform indicator, TMP_Text leftLabel, TMP_Text rightLabel, GameObject finishButtonContainer)
        {
            _sliderTrack = sliderTrack;
            _indicator = indicator;
            _leftLabel = leftLabel;
            _rightLabel = rightLabel;
            _finishButtonContainer = finishButtonContainer;

            if (_leftLabel != null)
                _leftLabel.text = "Уровень профессии";
            if (_rightLabel != null)
                _rightLabel.text = "% выполнения задания";
        }

        /// <summary>
        /// Запускает анимацию ползунка.
        /// getProfessionLevel - функция возврата уровня (0..1). По умолчанию 0.5.
        /// onComplete - вызывается с бонусным множителем (0.5..1.5) и финальными очками.
        /// </summary>
        public void Run(Func<float> getProfessionLevel, int baseScore, Action<int, float> onComplete)
        {
            if (_finishButtonContainer != null)
                _finishButtonContainer.SetActive(false);

            StartCoroutine(RunCoroutine(getProfessionLevel ?? (() => 0.5f), baseScore, onComplete));
        }

        private IEnumerator RunCoroutine(Func<float> getProfessionLevel, int baseScore, Action<int, float> onComplete)
        {
            if (_indicator == null || _sliderTrack == null)
            {
                if (_finishButtonContainer != null)
                    _finishButtonContainer.SetActive(true);
                onComplete?.Invoke(baseScore, 1f);
                yield break;
            }

            float trackWidth = _sliderTrack.sizeDelta.x;
            float halfWidth = trackWidth * 0.5f - 5f;
            float direction = 1f;
            float pos = 0f;
            float elapsed = 0f;

            while (elapsed < _sliderDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float speed = halfWidth * 4f / _sliderDuration;
                pos += direction * speed * dt;

                if (pos >= halfWidth)
                {
                    pos = halfWidth;
                    direction = -1f;
                }
                else if (pos <= -halfWidth)
                {
                    pos = -halfWidth;
                    direction = 1f;
                }

                _indicator.anchoredPosition = new Vector2(pos, 0);
                yield return null;
            }

            float level = getProfessionLevel();
            float targetPos = GetFinalPosition(level, halfWidth);

            float animTime = 0.15f;
            float animElapsed = 0f;
            Vector2 startPos = _indicator.anchoredPosition;

            while (animElapsed < animTime)
            {
                animElapsed += Time.deltaTime;
                float t = animElapsed / animTime;
                t = 1f - (1f - t) * (1f - t);
                float currentPos = Mathf.Lerp(startPos.x, targetPos, t);
                _indicator.anchoredPosition = new Vector2(currentPos, 0);
                yield return null;
            }

            _indicator.anchoredPosition = new Vector2(targetPos, 0);

            float normalizedDistance = Mathf.Abs(targetPos) / halfWidth;
            float bonus = 0.5f + (1f - normalizedDistance);
            int finalScore = Mathf.RoundToInt(baseScore * bonus);

            if (_finishButtonContainer != null)
                _finishButtonContainer.SetActive(true);

            onComplete?.Invoke(finalScore, bonus);
        }

        private float GetFinalPosition(float level, float halfWidth)
        {
            float random = UnityEngine.Random.value * 2f - 1f;
            float bias = 1f - Mathf.Clamp01(level) * 0.95f;
            float pos = random * bias * halfWidth;
            return Mathf.Clamp(pos, -halfWidth, halfWidth);
        }
    }
}
