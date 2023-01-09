using AppMana.InteractionToolkit.Animation;
using AppMana.InteractionToolkit.MaterialUI;
using UnityEngine;

namespace AppMana.InteractionToolkit
{
    [AddComponentMenu("UI/Screen", 101)]
    public class MaterialScreen : MonoBehaviour
    {
        [SerializeField]
        private bool m_OptionsControlledByScreenView = true;
        public bool optionsControlledByScreenView
        {
            get { return m_OptionsControlledByScreenView; }
            set { m_OptionsControlledByScreenView = value; }
        }

        [SerializeField]
        private bool m_DisableWhenNotVisible = true;
        public bool disableWhenNotVisible
        {
            get { return m_DisableWhenNotVisible; }
            set { m_DisableWhenNotVisible = value; }
        }

        //  Transition In
        [SerializeField]
        private bool m_FadeIn = true;
        public bool fadeIn
        {
            get { return m_FadeIn; }
            set { m_FadeIn = value; }
        }

        [SerializeField]
        private Tween.TweenType m_FadeInTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType fadeInTweenType
        {
            get { return m_FadeInTweenType; }
            set { m_FadeInTweenType = value; }
        }

        [SerializeField]
        private float m_FadeInAlpha;
        public float fadeInAlpha
        {
            get { return m_FadeInAlpha; }
            set { m_FadeInAlpha = value; }
        }

        private AnimationCurve m_FadeInCustomCurve;
        public AnimationCurve fadeInCustomCurve
        {
            get { return m_FadeInCustomCurve; }
            set { m_FadeInCustomCurve = value; }
        }

        [SerializeField]
        private bool m_ScaleIn;
        public bool scaleIn
        {
            get { return m_ScaleIn; }
            set { m_ScaleIn = value; }
        }

        [SerializeField]
        private Tween.TweenType m_ScaleInTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType scaleInTweenType
        {
            get { return m_ScaleInTweenType; }
            set { m_ScaleInTweenType = value; }
        }

        [SerializeField]
        private float m_ScaleInScale;
        public float scaleInScale
        {
            get { return m_ScaleInScale; }
            set { m_ScaleInScale = value; }
        }

        private AnimationCurve m_ScaleInCustomCurve;
        public AnimationCurve scaleInCustomCurve
        {
            get { return m_ScaleInCustomCurve; }
            set { m_ScaleInCustomCurve = value; }
        }

        [SerializeField]
        private bool m_SlideIn;
        public bool slideIn
        {
            get { return m_SlideIn; }
            set { m_SlideIn = value; }
        }

        [SerializeField]
        private Tween.TweenType m_SlideInTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType slideInTweenType
        {
            get { return m_SlideInTweenType; }
            set { m_SlideInTweenType = value; }
        }

        [SerializeField]
        private ScreenView.SlideDirection m_SlideInDirection = ScreenView.SlideDirection.Right;
        public ScreenView.SlideDirection slideInDirection
        {
            get { return m_SlideInDirection; }
            set { m_SlideInDirection = value; }
        }

        [SerializeField]
        private bool m_AutoSlideInAmount = true;
        public bool autoSlideInAmount
        {
            get { return m_AutoSlideInAmount; }
            set { m_AutoSlideInAmount = value; }
        }

        [SerializeField]
        private float m_SlideInAmount;
        public float slideInAmount
        {
            get { return m_SlideInAmount; }
            set { m_SlideInAmount = value; }
        }

        [SerializeField]
        private float m_SlideInPercent = 100f;
        public float slideInPercent
        {
            get { return m_SlideInPercent; }
            set { m_SlideInPercent = value; }
        }

        private AnimationCurve m_SlideInCustomCurve;
        public AnimationCurve slideInCustomCurve
        {
            get { return m_SlideInCustomCurve; }
            set { m_SlideInCustomCurve = value; }
        }

        //  Transition Out
        [SerializeField]
        private bool m_FadeOut;
        public bool fadeOut
        {
            get { return m_FadeOut; }
            set { m_FadeOut = value; }
        }

        [SerializeField]
        private Tween.TweenType m_FadeOutTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType fadeOutTweenType
        {
            get { return m_FadeOutTweenType; }
            set { m_FadeOutTweenType = value; }
        }

        [SerializeField]
        private float m_FadeOutAlpha;
        public float fadeOutAlpha
        {
            get { return m_FadeOutAlpha; }
            set { m_FadeOutAlpha = value; }
        }

        private AnimationCurve m_FadeOutCustomCurve;
        public AnimationCurve fadeOutCustomCurve
        {
            get { return m_FadeOutCustomCurve; }
            set { m_FadeOutCustomCurve = value; }
        }

        [SerializeField]
        private bool m_ScaleOut;
        public bool scaleOut
        {
            get { return m_ScaleOut; }
            set { m_ScaleOut = value; }
        }

        [SerializeField]
        private Tween.TweenType m_ScaleOutTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType scaleOutTweenType
        {
            get { return m_ScaleOutTweenType; }
            set { m_ScaleOutTweenType = value; }
        }

        [SerializeField]
        private float m_ScaleOutScale;
        public float scaleOutScale
        {
            get { return m_ScaleOutScale; }
            set { m_ScaleOutScale = value; }
        }

        private AnimationCurve m_ScaleOutCustomCurve;
        public AnimationCurve scaleOutCustomCurve
        {
            get { return m_ScaleOutCustomCurve; }
            set { m_ScaleOutCustomCurve = value; }
        }

        [SerializeField]
        private bool m_SlideOut;
        public bool slideOut
        {
            get { return m_SlideOut; }
            set { m_SlideOut = value; }
        }

        [SerializeField]
        private Tween.TweenType m_SlideOutTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType slideOutTweenType
        {
            get { return m_SlideOutTweenType; }
            set { m_SlideOutTweenType = value; }
        }

        [SerializeField]
        private ScreenView.SlideDirection m_SlideOutDirection = ScreenView.SlideDirection.Left;
        public ScreenView.SlideDirection slideOutDirection
        {
            get { return m_SlideOutDirection; }
            set { m_SlideOutDirection = value; }
        }

        [SerializeField]
        private bool m_AutoSlideOutAmount = true;
        public bool autoSlideOutAmount
        {
            get { return m_AutoSlideOutAmount; }
            set { m_AutoSlideOutAmount = value; }
        }

        [SerializeField]
        private float m_SlideOutAmount;
        public float slideOutAmount
        {
            get { return m_SlideOutAmount; }
            set { m_SlideOutAmount = value; }
        }

        [SerializeField]
        private float m_SlideOutPercent = 100f;
        public float slideOutPercent
        {
            get { return m_SlideOutPercent; }
            set { m_SlideOutPercent = value; }
        }

        private AnimationCurve m_SlideOutCustomCurve;
        public AnimationCurve slideOutCustomCurve
        {
            get { return m_SlideOutCustomCurve; }
            set { m_SlideOutCustomCurve = value; }
        }
        
        [SerializeField]
        private float m_TransitionDuration = 0.5f;
        public float transitionDuration
        {
            get { return m_TransitionDuration; }
            set { m_TransitionDuration = value; }
        }

        private ScreenView m_ScreenView;
        public ScreenView screenView
        {
            get
            {
                if (m_ScreenView == null)
                {
                    m_ScreenView = GetComponentInParent<ScreenView>();
                }

                return m_ScreenView;
            }
        }

        private RectTransform m_RectTransform;
        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = gameObject.GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        private CanvasGroup m_CanvasGroup;
        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = gameObject.GetAddComponent<CanvasGroup>();
                    m_CanvasGroup.blocksRaycasts = true;
                    m_CanvasGroup.interactable = true;
                    m_CanvasGroup.ignoreParentGroups = true;
                }
                return m_CanvasGroup;
            }
        }

        [SerializeField]
        private int m_ScreenIndex = -1;
        public int screenIndex
        {
            get { return m_ScreenIndex; }
            set { m_ScreenIndex = value; }
        }

        private int m_IsTransitioning = 0;
        private float m_TransitionStartTime;
        private float m_TransitionDeltaTime;

        private Vector2 m_TempRippleSize;
        private Vector3 m_TempRippleScale;
        private Vector3 m_TargetRipplePos;
        private Vector3 m_CurrentRipplePos;

        private Vector3 m_TempScreenPos;
        private Vector2 m_SlideScreenPos;

        private void CheckValues()
        {
            if (optionsControlledByScreenView)
            {
                fadeIn = screenView.fadeIn;
                fadeInTweenType = screenView.fadeInTweenType;
                fadeInAlpha = screenView.fadeInAlpha;
//                fadeInCustomCurve = screenView.fadeInCustomCurve;

                scaleIn = screenView.scaleIn;
                scaleInTweenType = screenView.scaleInTweenType;
                scaleInScale = screenView.scaleInScale;
//                scaleInCustomCurve = screenView.scaleInCustomCurve;

                slideIn = screenView.slideIn;
                slideInTweenType = screenView.slideInTweenType;
                slideInDirection = screenView.slideInDirection;
                autoSlideInAmount = screenView.autoSlideInAmount;
                slideInAmount = screenView.slideInAmount;
                slideInPercent = screenView.slideInPercent;
//                slideInCustomCurve = screenView.slideInCustomCurve;

                fadeOut = screenView.fadeOut;
                fadeOutTweenType = screenView.fadeOutTweenType;
                fadeOutAlpha = screenView.fadeOutAlpha;
//                fadeOutCustomCurve = screenView.fadeOutCustomCurve;

                scaleOut = screenView.scaleOut;
                scaleOutTweenType = screenView.scaleOutTweenType;
                scaleOutScale = screenView.scaleOutScale;
//                scaleOutCustomCurve = screenView.scaleOutCustomCurve;

                slideOut = screenView.slideOut;
                slideOutTweenType = screenView.slideOutTweenType;
                slideOutDirection = screenView.slideOutDirection;
                autoSlideOutAmount = screenView.autoSlideOutAmount;
                slideOutAmount = screenView.slideOutAmount;
                slideOutPercent = screenView.slideOutPercent;
//                slideOutCustomCurve = screenView.slideOutCustomCurve;

                transitionDuration = screenView.transitionDuration;
            }
        }

        public void Show()
        {
            Transition();
        }
        
        public void Transition()
        {
            screenView.Transition(this);
        }
        
        public void TransitionIn()
        {
            CheckValues();

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            gameObject.SetActive(true);

            m_TempScreenPos = rectTransform.position;
            
            if (fadeIn)
            {
                canvasGroup.alpha = fadeInAlpha;
            }
            if (scaleIn)
            {
                rectTransform.localScale = new Vector3(scaleInScale, scaleInScale, scaleInScale);
            }
            if (slideIn)
            {
                if (autoSlideInAmount)
                {
                    bool isVertical = (slideInDirection == ScreenView.SlideDirection.Up ||
                                       slideInDirection == ScreenView.SlideDirection.Down);

                    if (isVertical)
                    {
                        slideInAmount = rectTransform.GetProperSize().y * slideInPercent * 0.01f;
                    }
                    else
                    {
                        slideInAmount = rectTransform.GetProperSize().x * slideInPercent * 0.01f;
                    }
                }

                switch (slideInDirection)
                {
                    case ScreenView.SlideDirection.Left:
                        rectTransform.position = new Vector2(m_TempScreenPos.x - slideInAmount, m_TempScreenPos.y);
                        break;
                    case ScreenView.SlideDirection.Right:
                        rectTransform.position = new Vector2(m_TempScreenPos.x + slideInAmount, m_TempScreenPos.y);
                        break;
                    case ScreenView.SlideDirection.Up:
                        rectTransform.position = new Vector2(m_TempScreenPos.x, m_TempScreenPos.y + slideInAmount);
                        break;
                    case ScreenView.SlideDirection.Down:
                        rectTransform.position = new Vector2(m_TempScreenPos.x, m_TempScreenPos.y - slideInAmount);
                        break;
                }

                m_SlideScreenPos = rectTransform.position;
            }

            m_IsTransitioning = 1;
            m_TransitionStartTime = Time.realtimeSinceStartup;
        }

        public void TransitionOut()
        {
            CheckValues();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            m_TempScreenPos = rectTransform.position;
            
            if (fadeOut)
            {
                canvasGroup.alpha = 1f;
            }
            if (scaleOut)
            {
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
            }
            if (slideOut)
            {
                if (autoSlideOutAmount)
                {
                    bool isVertical = (slideOutDirection == ScreenView.SlideDirection.Up ||
                                       slideOutDirection == ScreenView.SlideDirection.Down);

                    if (isVertical)
                    {
                        slideOutAmount = rectTransform.GetProperSize().y * slideOutPercent * 0.01f;
                    }
                    else
                    {
                        slideOutAmount = rectTransform.GetProperSize().x * slideOutPercent * 0.01f;
                    }
                }

                switch (slideOutDirection)
                {
                    case ScreenView.SlideDirection.Left:
                        m_SlideScreenPos = new Vector2(m_TempScreenPos.x - slideOutAmount, m_TempScreenPos.y);
                        break;
                    case ScreenView.SlideDirection.Right:
                        m_SlideScreenPos = new Vector2(m_TempScreenPos.x + slideOutAmount, m_TempScreenPos.y);
                        break;
                    case ScreenView.SlideDirection.Up:
                        m_SlideScreenPos = new Vector2(m_TempScreenPos.x, m_TempScreenPos.y + slideOutAmount);
                        break;
                    case ScreenView.SlideDirection.Down:
                        m_SlideScreenPos = new Vector2(m_TempScreenPos.x, m_TempScreenPos.y - slideOutAmount);
                        break;
                }
            }

            m_IsTransitioning = 2;
            m_TransitionStartTime = Time.realtimeSinceStartup;
        }

        public void TransitionOutWithoutTransition()
        {
            m_IsTransitioning = 3;
            m_TransitionStartTime = Time.realtimeSinceStartup;
        }

        void Update()
        {
            if (m_IsTransitioning > 0)
            {
                m_TransitionDeltaTime = Time.realtimeSinceStartup - m_TransitionStartTime;

                if (m_TransitionDeltaTime <= transitionDuration)
                {
                    if (m_IsTransitioning == 1)
                    {
                        if (fadeIn)
                        {
                            canvasGroup.alpha = Tween.Evaluate(fadeInTweenType, fadeInAlpha, 1f, m_TransitionDeltaTime,
                                transitionDuration, fadeInCustomCurve);
                        }
                        if (scaleIn)
                        {
                            Vector3 tempVector3 = rectTransform.localScale;
                            tempVector3.x = Tween.Evaluate(scaleInTweenType, scaleInScale, 1f, m_TransitionDeltaTime,
                                transitionDuration, scaleInCustomCurve);
                            tempVector3.y = tempVector3.x;
                            tempVector3.z = tempVector3.x;
                            rectTransform.localScale = tempVector3;
                        }
                        if (slideIn)
                        {
                            Vector3 tempVector3 = rectTransform.position;
                            tempVector3.x = Tween.Evaluate(slideInTweenType, m_SlideScreenPos.x, m_TempScreenPos.x, m_TransitionDeltaTime,
                                transitionDuration, slideInCustomCurve);
                            tempVector3.y = Tween.Evaluate(slideInTweenType, m_SlideScreenPos.y, m_TempScreenPos.y, m_TransitionDeltaTime,
                                transitionDuration, slideInCustomCurve);
                            rectTransform.position = tempVector3;
                        }
                    }
                    else if (m_IsTransitioning == 2)
                    {
                        if (fadeOut)
                        {
                            canvasGroup.alpha = Tween.Evaluate(fadeOutTweenType, 1f, fadeOutAlpha,
                                m_TransitionDeltaTime, transitionDuration, fadeOutCustomCurve);
                        }
                        if (scaleOut)
                        {
                            Vector3 tempVector3 = rectTransform.localScale;
                            tempVector3.x = Tween.Evaluate(scaleOutTweenType, 1f, scaleOutScale, m_TransitionDeltaTime,
                                transitionDuration, scaleOutCustomCurve);
                            tempVector3.y = tempVector3.x;
                            tempVector3.z = tempVector3.x;
                            rectTransform.localScale = tempVector3;
                        }
                        if (slideOut)
                        {
                            Vector3 tempVector3 = rectTransform.position;
                            tempVector3.x = Tween.Evaluate(slideOutTweenType, m_TempScreenPos.x, m_SlideScreenPos.x,
                                m_TransitionDeltaTime, transitionDuration, slideOutCustomCurve);
                            tempVector3.y = Tween.Evaluate(slideOutTweenType, m_TempScreenPos.y, m_SlideScreenPos.y, m_TransitionDeltaTime,
                                transitionDuration, slideOutCustomCurve);
                            rectTransform.position = tempVector3;
                        }
                    }
                }
                else
                {
                    if (m_IsTransitioning == 1)
                    {
                        if (fadeIn)
                        {
                            canvasGroup.alpha = 1f;
                        }
                        if (scaleIn)
                        {
                            rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        }
                        if (slideIn)
                        {
                            rectTransform.position = m_TempScreenPos;
                        }
                    }
                    else if (m_IsTransitioning == 2)
                    {
                        if (fadeOut)
                        {
                            canvasGroup.alpha = 1f;
                        }
                        if (scaleOut)
                        {
                            rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        }
                        if (slideOut)
                        {
                            rectTransform.position = m_TempScreenPos;
                        }
                    }

                    if (m_IsTransitioning > 1)
                    {
                        if (m_DisableWhenNotVisible)
                        {
                            gameObject.SetActive(false);
                        }
                    }

                    m_IsTransitioning = 0;
                    screenView.OnScreenEndTransition(screenIndex);
                }
            }
        }

        public void Interrupt()
        {
            if (m_IsTransitioning == 1)
            {
                if (fadeIn)
                {
                    canvasGroup.alpha = 1f;
                }
                if (scaleIn)
                {
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                }
                if (slideIn)
                {
                    rectTransform.position = m_TempScreenPos;
                }
            }
            else if (m_IsTransitioning == 2)
            {
                if (fadeOut)
                {
                    canvasGroup.alpha = 1f;
                }
                if (scaleOut)
                {
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                }
                if (slideOut)
                {
                    rectTransform.position = m_TempScreenPos;
                }
            }

            if (m_IsTransitioning > 1)
            {
                if (m_DisableWhenNotVisible)
                {
                    gameObject.SetActive(false);
                }
            }

            if (m_IsTransitioning > 0)
            {
                m_IsTransitioning = 0;
                screenView.OnScreenEndTransition(screenIndex);
            }
        }
    }
}