﻿using CCGKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GrandDevs.CZB
{
    public class GameplayManager : IService, IGameplayManager
    {
        private List<IController> _controllers;

        public int PlayerHeroId { get; set; }
        public int OpponentHeroId { get; set; }


        public void Dispose()
        {
            foreach (var item in _controllers)
                item.Dispose();
        }

        public void Init()
        {
            InitControllers();
        }

        public void Update()
        {
            foreach (var item in _controllers)
                item.Update();
        }

        public T GetController<T>() where T : IController
        {
            return (T)_controllers.Find(x => x is T);
        }

        private void InitControllers()
        {
            _controllers = new List<IController>();
            _controllers.Add(new AbilitiesController());
        }

        public string GetCardSet(Data.Card card)
        {
            foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
            {
                if (cardSet.cards.IndexOf(card) > -1)
                    return cardSet.name;
            }

            return string.Empty;
        }

        public void RearrangeHands()
        {
            (NetworkingUtils.GetHumanLocalPlayer() as DemoHumanPlayer).RearrangeBottomBoard();
            (NetworkingUtils.GetHumanLocalPlayer() as DemoHumanPlayer).RearrangeTopBoard();
        }
    }
}
