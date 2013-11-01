﻿using Dominion;
using Dominion.Strategy;
using CardTypes = Dominion.CardTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Program;

namespace Strategies
{    
    public class BigMoneyWithCard
        : Strategy
    {
            
        public static PlayerAction Player(Card card, 
            string playerName = null, 
            int cardCount = 1, 
            int afterSilverCount = 0,                 
            int countGoldBeforeProvince = 3,
            int countRemainingProvinceBeforeDuchy = 4,
            int countRemainingProvinceBeforeEstateOverGold = 1,
            int countRemainingProvinceBeforeEstateOverSilver = 3,
            int afterGoldCount = int.MaxValue)
        {
            return new PlayerAction(
                        playerName == null ? "BigMoneyWithCard" + card.GetType().Name : playerName,                            
                        purchaseOrder: PurchaseOrder(
                            card, 
                            cardCount, 
                            afterSilverCount, 
                            afterGoldCount == int.MaxValue && card.DefaultCoinCost >= Cards.Gold.DefaultCoinCost ? 0 : afterGoldCount,
                            countGoldBeforeProvince,
                            countRemainingProvinceBeforeDuchy,
                            countRemainingProvinceBeforeEstateOverGold,
                            countRemainingProvinceBeforeEstateOverSilver),
                        actionOrder:ActionOrder(card));
        }

        public static ICardPicker PurchaseOrder(
            Card card, 
            int cardCount, 
            int afterSilverCount, 
            int afterGoldCount, 
            int countGoldBeforeProvince, 
            int countRemainingProvinceBeforeDuchy,
            int countRemainingProvinceBeforeEstateOverGold,
            int countRemainingProvinceBeforeEstateOverSilver)
        {
            return new CardPickByPriority(
                        CardAcceptance.For(Cards.Province, gameState => CountAllOwned(Cards.Gold, gameState) >= countGoldBeforeProvince),
                        CardAcceptance.For(Cards.Duchy, gameState => CountOfPile(Cards.Province, gameState) <= countRemainingProvinceBeforeDuchy),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) <= countRemainingProvinceBeforeEstateOverGold),
                        CardAcceptance.For(card, gameState => CountAllOwned(card, gameState) < cardCount && CountAllOwned(Cards.Gold, gameState) >= afterGoldCount),                           
                        CardAcceptance.For(Cards.Gold),
                        CardAcceptance.For(card, gameState => CountAllOwned(card, gameState) < cardCount && CountAllOwned(Cards.Silver, gameState) >= afterSilverCount),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) <= 3),
                        CardAcceptance.For(Cards.Potion, 1, gameState => card.potionCost > 0),
                        CardAcceptance.For(Cards.Silver));
        }

        public static ICardPicker ActionOrder(Card card)
        {
            return new CardPickByPriority(
                CardAcceptance.For(card));
        }            
    }
        

    public class BigMoneyWharf
        : Strategy
    {            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Wharf, "BigMoneyWharf");
        }
    }

    public class BigMoneyBridge
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Bridge, "BigMoneyBridge");
        }
    }        

    public class BigMoneySingleSmithy
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Smithy, "BigMoneySingleSmithy");
        }
    }

    public class BigMoneySingleWitch
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Witch, "BigMoneySingleWitch", afterGoldCount:0);
        }
    }

    public class BigMoneyDoubleWitch
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Witch, "BigMoneyDoubleWitch", cardCount: 2, afterGoldCount: 0);
        }
    }

    public class BigMoneyMoneylender
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return BigMoneyWithCard.Player(Cards.Moneylender);
        }
    } 

    public class BigMoneyWithThief          
        : Strategy
    {
            
        public static PlayerAction Player()
        {
            return CustomPlayer();                
        }

        public static PlayerAction CustomPlayer(int cardCount = 1)
        {
            return new MyPlayerAction(cardCount);                            
        }

        class MyPlayerAction
            : PlayerAction
        {
            public MyPlayerAction(int cardCount)
                : base("Thief",                           
                        purchaseOrder: PurchaseOrder(cardCount),                            
                        trashOrder: TrashOrder())
            {
            }                
        }

        public static ICardPicker PurchaseOrder(int cardCount)
        {
            return new CardPickByPriority(
                        CardAcceptance.For(Cards.Province),
                        CardAcceptance.For(Cards.Duchy, gameState => CountOfPile(Cards.Province, gameState) < 4),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) < 2),
                        CardAcceptance.For(Cards.Gold),
                        CardAcceptance.For(Cards.Thief, gameState => CountAllOwned(Cards.Thief, gameState) < cardCount && CountAllOwned(Cards.Silver, gameState) >= 2),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) < 4),
                        CardAcceptance.For(Cards.Silver));

        }

        private static ICardPicker TrashOrder()
        {
            return new CardPickByPriority(
                        CardAcceptance.For(Cards.Gold),
                        CardAcceptance.For(Cards.Silver),
                        CardAcceptance.For(Cards.Copper));
        }
    }


    public class BigMoneySingleCardCartographer
        : Strategy
    {
            
        public static PlayerAction Player(Card card, int cardCount = 1)
        {
            return new PlayerAction(
                        "BigMoneySingleCardCartographer",                            
                        purchaseOrder: PurchaseOrder(card, cardCount),                            
                        actionOrder: ActionOrder(card));
        }

        private static ICardPicker PurchaseOrder(Card card, int cardCount)
        {
            return new CardPickByPriority(
                        CardAcceptance.For(Cards.Province, gameState => CountAllOwned(Cards.Gold, gameState) > 2),
                        CardAcceptance.For(Cards.Duchy, gameState => CountOfPile(Cards.Province, gameState) < 4),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) < 2),
                        CardAcceptance.For(card, gameState => CountAllOwned(card, gameState) < cardCount),
                        CardAcceptance.For(Cards.Gold),
                        CardAcceptance.For(Cards.Cartographer),
                        CardAcceptance.For(Cards.Estate, gameState => CountOfPile(Cards.Province, gameState) < 4),
                        CardAcceptance.For(Cards.Silver));

        }

        private static ICardPicker ActionOrder(Card card)
        {
            return new CardPickByPriority(
                        CardAcceptance.For(Cards.Cartographer),
                        CardAcceptance.For(card));
        }
    }
}
