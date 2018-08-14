// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using UnityEngine;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using LoomNetwork.CZB.Common;

namespace LoomNetwork.CZB
{
    public class PlayerManaBarItem
    {
        private GameObject _selfObject,
                            _arrowObject,
            
                            _overflowObject,
                            _gooMeterObject;

        private TextMeshPro _gooAmountText,
                            _overflowGooAmountText;
        private List<GooBottleItem> _gooBottles;

        private Transform _overflowBottleContainer;

        private int _maxValue, _currentValue;

        private const int _meterArrowStep = 12;

        private Vector3 _overflowPos;

        private string _overflowPrefabPath;

        private bool _isInOverflow = false;

        public PlayerManaBarItem() { }

        public PlayerManaBarItem(GameObject gameObject, string overflowPrefabName, Vector3 overflowPos)
        {
            _overflowPrefabPath = "Prefabs/" + overflowPrefabName;
            _overflowPos = overflowPos;
            _selfObject = gameObject;
            _gooMeterObject = _selfObject.transform.Find("GooMeter").gameObject;
            _gooAmountText = _gooMeterObject.transform.Find("Text").GetComponent<TextMeshPro>();
            _arrowObject = _gooMeterObject.transform.Find("ArrowCenter").gameObject;
            _gooBottles = new List<GooBottleItem>();
            GameObject bottle = null;
            for (int i = 0; i < _selfObject.transform.childCount; i++)
            {
                bottle = _selfObject.transform.GetChild(i).gameObject;
                if (bottle.name.Contains("BottleGoo"))
                    _gooBottles.Add(new GooBottleItem(bottle));
            }

            _isInOverflow = false;

            _arrowObject.transform.localEulerAngles = Vector3.forward * 90;

            GameClient.Get<IGameplayManager>().OnGameEndedEvent += OnGameEndedEventHandler;
        }

        public void SetGoo(int gooValue)
        {
            _currentValue = gooValue;
            _gooAmountText.text = _currentValue.ToString() + "/" + _maxValue;

            UpdateGooOVerflow();

            for (var i = 0; i < _gooBottles.Count; i++)
            {
                if (i < _currentValue)
                    Active(_gooBottles[i]);
                else
                   Disactive(_gooBottles[i]);
            }
            UpdateGooMeter();
        }

       

        public void SetVialGoo(int maxValue)
        {
            _maxValue = maxValue;
            _gooAmountText.text = _currentValue.ToString() + "/" + _maxValue;
            for (var i = 0; i < _gooBottles.Count; i++)
            {
                _gooBottles[i].self.SetActive(i < _maxValue ? true : false);
            }
            UpdateGooOVerflow();
        }

        private void UpdateGooOVerflow()
        {
            if (_currentValue > _maxValue && !_isInOverflow)
            {
                CreateOverflow();

                _isInOverflow = true;
            }
            else if (_currentValue <= _maxValue && _isInOverflow)
            {
                DestroyOverflow();

                _isInOverflow = false;
            }
            if (_overflowGooAmountText != null)
            {
                _overflowGooAmountText.text = _currentValue.ToString() + "/" + _maxValue;
                for (int i = 0; i < _overflowBottleContainer.childCount; i++)
                {
                    _overflowBottleContainer.GetChild(i).gameObject.SetActive(i < _currentValue ? true : false); ;
                }
            }
        }

        public void Active(GooBottleItem item)
        {
            item.fullBoottle.DOFade(1.0f, 0.5f);
            item.glowBottle.DOFade(1.0f, 0.5f);
        }

        public void Disactive(GooBottleItem item)
        {
            item.fullBoottle.DOFade(0.0f, 0.5f);
            item.glowBottle.DOFade(0.0f, 0.5f);
        }

        private void UpdateGooMeter()
        {
            int targetRotation = 90 - _meterArrowStep * _currentValue;
            if (targetRotation < -90)
                targetRotation = -90;
            _arrowObject.transform.DORotate(Vector3.forward * targetRotation, 1f);
            //_arrowObject.transform.localEulerAngles = Vector3.forward * (90 - _meterArrowStep);
        }

        private void CreateOverflow()
        {
            _overflowObject = MonoBehaviour.Instantiate<GameObject>(GameClient.Get<ILoadObjectsManager>().GetObjectByPath<GameObject>(_overflowPrefabPath));
            _overflowObject.transform.localPosition = _overflowPos;
            _overflowGooAmountText = _overflowObject.transform.Find("clock/Text").GetComponent<TextMeshPro>();
            _overflowBottleContainer = _overflowObject.transform.Find("Bottles").transform;
            for (int i = 0; i < _overflowBottleContainer.childCount; i++)
            {
                _overflowBottleContainer.GetChild(i).gameObject.SetActive(i < _currentValue ? true : false); ;
            }
            _selfObject.SetActive(false);

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.GOO_OVERFLOW_FADE_IN, Constants.BATTLEGROUND_EFFECTS_SOUND_VOLUME);

            GameClient.Get<ITimerManager>().AddTimer(PlayOverflowLoopDelay, null, GameClient.Get<ISoundManager>().GetSoundLength(Enumerators.SoundType.GOO_OVERFLOW_FADE_IN));
        }

        private void PlayOverflowLoopDelay(object[] param)
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.GOO_OVERFLOW_FADE_LOOP, Constants.BATTLEGROUND_EFFECTS_SOUND_VOLUME, true);
        }

        private void DestroyOverflow()
        {
            MonoBehaviour.Destroy(_overflowObject);
            _overflowObject = null;
            _overflowBottleContainer = null;
            _overflowGooAmountText = null;
            _selfObject.SetActive(true);

            StopOverfowSounds();

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.GOO_OVERFLOW_FADE_OUT, Constants.BATTLEGROUND_EFFECTS_SOUND_VOLUME);
        }


        private void StopOverfowSounds()
        {
            GameClient.Get<ITimerManager>().StopTimer(PlayOverflowLoopDelay);

            GameClient.Get<ISoundManager>().StopPlaying(Enumerators.SoundType.GOO_OVERFLOW_FADE_IN);
            GameClient.Get<ISoundManager>().StopPlaying(Enumerators.SoundType.GOO_OVERFLOW_FADE_LOOP);
            GameClient.Get<ISoundManager>().StopPlaying(Enumerators.SoundType.GOO_OVERFLOW_FADE_OUT);
        }

        private void OnGameEndedEventHandler(Enumerators.EndGameType obj)
        {
            StopOverfowSounds();
            _gooMeterObject.SetActive(false);

            _isInOverflow = false;

            GameClient.Get<IGameplayManager>().OnGameEndedEvent -= OnGameEndedEventHandler;
        }

        public struct GooBottleItem
        {
            public SpriteRenderer fullBoottle,
                                   glowBottle;
            public GameObject self;


            public GooBottleItem(GameObject gameObject)
            {
                self = gameObject;
                fullBoottle = self.transform.Find("Goo").GetComponent<SpriteRenderer>();
                glowBottle = self.transform.Find("BottleGlow").GetComponent<SpriteRenderer>();
            }
        }
    }
}