using UnityEngine;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Plays a sequential sprite frame animation on a SpriteRenderer and auto-destroys on completion.
    /// Works independently of Unity Animator for lightweight visual effects like explosions and thrusters.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class AnimatedSpriteEffect : MonoBehaviour
    {
        [Header("Frames & Timing")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 15f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool autoDestroy = true;

        private SpriteRenderer sr;
        private float timer = 0f;
        private int currentFrame = 0;

        public Sprite[] Frames
        {
            get => frames;
            set => frames = value;
        }

        public float FramesPerSecond
        {
            get => framesPerSecond;
            set => framesPerSecond = Mathf.Max(1f, value);
        }

        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        public bool AutoDestroy
        {
            get => autoDestroy;
            set => autoDestroy = value;
        }

        public SpriteRenderer SpriteRendererComponent
        {
            get
            {
                if (sr == null) sr = GetComponent<SpriteRenderer>();
                return sr;
            }
        }

        private void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0 && sr != null)
            {
                sr.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || sr == null) return;

            timer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;

            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        currentFrame = frames.Length - 1;
                        if (autoDestroy)
                        {
                            Destroy(gameObject);
                            return;
                        }
                    }
                }

                sr.sprite = frames[currentFrame];
            }
        }

        /// <summary>
        /// Manually ticks animation for EditMode testing and deterministic simulation.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0 || sr == null) return;

            timer += deltaTime;
            float frameDuration = 1f / framesPerSecond;

            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        currentFrame = frames.Length - 1;
                        break;
                    }
                }
            }

            if (currentFrame >= 0 && currentFrame < frames.Length)
            {
                sr.sprite = frames[currentFrame];
            }
        }
    }
}
