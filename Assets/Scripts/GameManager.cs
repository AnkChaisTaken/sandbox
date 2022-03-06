using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    System.Random random = new System.Random();

    public Sprite pixelSprite;

    public ParticleType[] particleTypes = new ParticleType[]
    {
        new ParticleType(0, "Air", new Color32(0, 0, 0, 80), 0),
        new ParticleType(1, "Sand", new Color32(255, 239, 66, 255), 3),
        new ParticleType(2, "Water", new Color32(34, 56, 199, 255), 2),
        new ParticleType(3, "Stone", new Color32(40, 40, 40, 255), 3),
        new ParticleType(4, "Granite", new Color32(84, 50, 68, 255), 3),
        new ParticleType(5, "Oil", new Color32(47, 64, 38, 255), 1),
        new ParticleType(6, "Fire", new Color32(255, 51, 0, 255), 3),
        new ParticleType(7, "Smoke", new Color32(20, 20, 20, 255), 3)
    };

    public List<Particle> particles = new List<Particle>();
    public Pixel[,] space;

    int currentParticleID;
    bool isGameRunning = true;

    private void Awake()
    {
        if (instance != null)
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeArea(71, 40);

        Thread handlePhysics = new Thread(HandlePhysics);
        handlePhysics.Start();
    }

    void InitializeArea(int _width, int _height)
    {
        space = new Pixel[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Pixel _pixel = new Pixel();

                GameObject _gameObject = new GameObject();
                _gameObject.name = "Particle";
                _gameObject.transform.position = new Vector2(x, y);
                _gameObject.AddComponent<SpriteRenderer>().sprite = pixelSprite;

                _pixel.id = 0;
                _pixel.position = new Vector2(x, y);
                _pixel.gameObject = _gameObject;
                _pixel.spriteRenderer = _gameObject.GetComponent<SpriteRenderer>();

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    _pixel.spriteRenderer.color = particleTypes[0].color;
                });

                _gameObject.transform.parent = this.transform;

                space[x, y] = _pixel;
            }
        }
    }

    private void OnApplicationQuit()
    {
        isGameRunning = false;
    }

    void CreateParticle(int _x, int _y, byte _id)
    {
        if (_id != 0)
        {
            if (space[_x, _y].particle != null)
            {
                RemoveParticle(space[_x, _y].particle);
            }

            space[_x, _y].id = _id;

            Particle _particle = new Particle();
            _particle.id = _id;
            _particle.isSet = false;
            _particle.pixel = space[_x, _y];

            space[_x, _y].particle = _particle;

            particles.Add(_particle);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                //space[_x, _y].spriteRenderer.color = particleTypes[_id].color;
                space[_x, _y].spriteRenderer.color = new Color32((byte)(particleTypes[_id].color.r + random.Next(-10, 10)), (byte)(particleTypes[_id].color.g + random.Next(-5, 5)), (byte)(particleTypes[_id].color.b + random.Next(-5, 5)), particleTypes[_id].color.a);
            });
        }
    }

    void RemoveParticle(Particle _particle)
    {
        if (particles.Contains(_particle))
        {
            _particle.pixel.particle = null;
            _particle.pixel.id = 0;
            ThreadManager.ExecuteOnMainThread(() =>
            {
                _particle.pixel.spriteRenderer.color = particleTypes[0].color;
            });

            particles.Remove(_particle);
        }
    }

    void MoveParticle(Particle _particle, int _x, int _y)
    {
        Particle _oldParticle = space[_x, _y].particle;

        byte _particleID = _particle.id;
        byte _oldParticleID = 0;

        if (_oldParticle != null)
        {
            _oldParticleID = _oldParticle.id;
        }

        Pixel _oldPixel = _particle.pixel;
        Pixel _newPixel = space[_x, _y];

        if (_oldParticle != null && _oldParticleID != 0)
        {
            _oldPixel.id = _oldParticleID;
            _oldPixel.particle = _oldParticle;
            ThreadManager.ExecuteOnMainThread(() =>
            {
                _oldPixel.spriteRenderer.color = particleTypes[_oldParticleID].color;
            });

            _oldParticle.pixel = _oldPixel;
        }
        else
        {
            _oldPixel.id = 0;
            _oldPixel.particle = null;
            ThreadManager.ExecuteOnMainThread(() =>
            {
                _oldPixel.spriteRenderer.color = particleTypes[0].color;
            });
        }

        _particle.pixel = _newPixel;

        _newPixel.id = _particleID; ;
        _newPixel.particle = _particle;
        ThreadManager.ExecuteOnMainThread(() =>
        {
            _newPixel.spriteRenderer.color = particleTypes[_particleID].color;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 _rawMousePosition = Input.mousePosition;
            _rawMousePosition.z = Camera.main.nearClipPlane;
            Vector2 _mousePosition = Camera.main.ScreenToWorldPoint(_rawMousePosition);

            foreach (Pixel _pixel in space)
            {
                if (_mousePosition.x < _pixel.gameObject.transform.position.x + (_pixel.gameObject.transform.localScale.x / 2) && _mousePosition.x > _pixel.gameObject.transform.position.x - (_pixel.gameObject.transform.localScale.x / 2) && _mousePosition.y > _pixel.gameObject.transform.position.y - (_pixel.gameObject.transform.localScale.y / 2) && _mousePosition.y < _pixel.gameObject.transform.position.y + (_pixel.gameObject.transform.localScale.y / 2))
                {
                    if (space[(int)_pixel.position.x, (int)_pixel.position.y].id == 0 || currentParticleID == 0)
                    {
                        CreateParticle((int)_pixel.position.x, (int)_pixel.position.y, (byte)currentParticleID);
                    }
                    break;
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 _rawMousePosition = Input.mousePosition;
            _rawMousePosition.z = Camera.main.nearClipPlane;
            Vector2 _mousePosition = Camera.main.ScreenToWorldPoint(_rawMousePosition);

            foreach (Pixel _pixel in space)
            {
                if (_mousePosition.x < _pixel.gameObject.transform.position.x + (_pixel.gameObject.transform.localScale.x / 2) && _mousePosition.x > _pixel.gameObject.transform.position.x - (_pixel.gameObject.transform.localScale.x / 2) && _mousePosition.y > _pixel.gameObject.transform.position.y - (_pixel.gameObject.transform.localScale.y / 2) && _mousePosition.y < _pixel.gameObject.transform.position.y + (_pixel.gameObject.transform.localScale.y / 2))
                {
                    if (space[(int)_pixel.position.x, (int)_pixel.position.y].particle != null)
                    {
                        RemoveParticle(space[(int)_pixel.position.x, (int)_pixel.position.y].particle);
                    }
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            currentParticleID = 0;
        }else if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentParticleID = 1;
        }else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentParticleID = 2;
        }else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentParticleID = 3;
        }else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentParticleID = 4;
        }else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            currentParticleID = 5;
        }else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            currentParticleID = 6;
        }else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            currentParticleID = 7;
        }
    }

    void HandlePhysics()
    {
        while (isGameRunning == true)
        {
            Thread.Sleep(64);

            for(int i = 0; i < particles.Count; i++)
            {
                Particle _particle = particles[i];

                int _positionX = (int)_particle.pixel.position.x;
                int _positionY = (int)_particle.pixel.position.y;

                int _direction = 0;

                switch (_particle.id)
                {
                    case 1:
                        if (CanMove(_positionX, _positionY - 1, _particle.id))
                        {
                            MoveParticle(_particle, _positionX, _positionY - 1);
                        }
                        else if (CanMove(_positionX + 1, _positionY - 1, _particle.id) && CanMove(_positionX + 1, _positionY, _particle.id))
                        {
                            MoveParticle(_particle, _positionX + 1, _positionY - 1);
                        }
                        else if (CanMove(_positionX - 1, _positionY - 1, _particle.id) && CanMove(_positionX - 1, _positionY, _particle.id))
                        {
                            MoveParticle(_particle, _positionX - 1, _positionY - 1);
                        }
                        break;

                    case 2:
                        if (CanMove(_positionX, _positionY - 1, _particle.id))
                        {
                            MoveParticle(_particle, _positionX, _positionY - 1);
                        }
                        else
                        {
                            _direction = random.Next(0, 2);

                            if (_direction == 0)
                            {
                                if (CanMove(_positionX + 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX + 1, _positionY);
                                }
                                else if (CanMove(_positionX - 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX - 1, _positionY);
                                }
                            }
                            else
                            {
                                if (CanMove(_positionX - 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX - 1, _positionY);
                                }
                                else if (CanMove(_positionX + 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX + 1, _positionY);
                                }
                            }
                        }
                        break;

                    case 4:
                        if (CanMove(_positionX, _positionY - 1, _particle.id))
                        {
                            MoveParticle(_particle, _positionX, _positionY - 1);
                        }
                        break;

                    case 5:
                        if (CanMove(_positionX, _positionY - 1, _particle.id))
                        {
                            MoveParticle(_particle, _positionX, _positionY - 1);
                        }
                        else
                        {
                            _direction = random.Next(0, 2);

                            if (_direction == 0)
                            {
                                if (CanMove(_positionX + 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX + 1, _positionY);
                                }
                                else if (CanMove(_positionX - 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX - 1, _positionY);
                                }
                            }
                            else
                            {
                                if (CanMove(_positionX - 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX - 1, _positionY);
                                }
                                else if (CanMove(_positionX + 1, _positionY, _particle.id))
                                {
                                    MoveParticle(_particle, _positionX + 1, _positionY);
                                }
                            }
                        }
                        break;

                    case 6:
                        if (CanMove(_positionX, _positionY - 1, _particle.id))
                        {
                            if (space[_positionX, _positionY - 1].id == 5)
                            {
                                RemoveParticle(space[_positionX, _positionY].particle);
                                CreateParticle(_positionX, _positionY - 1, 6);
                                CreateParticle(_positionX, _positionY, 7);
                            }
                        }

                        if (CanMove(_positionX, _positionY + 1, _particle.id))
                        {
                            if (space[_positionX, _positionY + 1].id == 5)
                            {
                                RemoveParticle(space[_positionX, _positionY].particle);
                                CreateParticle(_positionX, _positionY + 1, 6);
                                CreateParticle(_positionX, _positionY, 7);
                            }
                        }

                        if (CanMove(_positionX + 1, _positionY, _particle.id))
                        {
                            if (space[_positionX + 1, _positionY].id == 5)
                            {
                                RemoveParticle(space[_positionX, _positionY].particle);
                                CreateParticle(_positionX + 1, _positionY, 6);
                                CreateParticle(_positionX, _positionY, 7);
                            }
                        }

                        if (CanMove(_positionX - 1, _positionY, _particle.id))
                        {
                            if (space[_positionX - 1, _positionY].id == 5)
                            {
                                RemoveParticle(space[_positionX, _positionY].particle);
                                CreateParticle(_positionX - 1, _positionY, 6);
                                CreateParticle(_positionX, _positionY, 7);
                            }
                        }

                        RemoveParticle(_particle);
                        break;

                    case 7:
                        if (CanMove(_positionX, _positionY + 1, _particle.id))
                        {
                            MoveParticle(_particle, _positionX, _positionY + 1);
                        }

                        _direction = random.Next(0, 3);

                        if(_direction == 0)
                        {
                            if (CanMove(_positionX + 1, _positionY, _particle.id))
                            {
                                MoveParticle(_particle, _positionX + 1, _positionY);
                            }
                        }
                        else if(_direction == 1)
                        {
                            if (CanMove(_positionX - 1, _positionY, _particle.id))
                            {
                                MoveParticle(_particle, _positionX - 1, _positionY);
                            }
                        }
                        break;
                }
            }
        }
    }

    bool CanMove(int _x, int _y, byte _id)
    {
        if(_x >= 0 && _x < space.GetLength(0) && _y >= 0 && _y < space.GetLength(1))
        {
            if (space[_x, _y].id != _id)
            {
                if (particleTypes[space[_x, _y].id].intensity < particleTypes[_id].intensity)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }else
        {
            return false;
        }
    }

    public class ParticleType
    {
        byte id;
        public string name;
        public Color32 color;
        public int intensity;

        public ParticleType(byte _id, string _name, Color32 _color, int _intensity)
        {
            id = _id;
            name = _name;
            color = _color;
            intensity = _intensity;
        }
    }

    public class Pixel
    {
        public byte id;
        public Vector2 position;
        public Particle particle;
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
    }

    public class Particle
    {
        public byte id;
        public Pixel pixel;

        public bool isSet;
    }
}

