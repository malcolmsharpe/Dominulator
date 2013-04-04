﻿using Dominion;
using CardTypes = Dominion.CardTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            ComparePlayers(Strategies.DeathCart.Player(1), Strategies.BigMoney.Player(2));
        }

        static void ComparePlayers(PlayerAction player1, PlayerAction player2)
        {
            int numberOfGames = 1000;

            int[] winnerCount = new int[2];
            int tieCount = 0;

            for (int gameCount = 0; gameCount < numberOfGames; ++gameCount)
            {
                using (IGameLog gameLog = gameCount == 0 ? (IGameLog)new HumanReadableGameLog("..\\..\\Results\\GameLog.txt") : (IGameLog)new EmptyGameLog())
                //using (IGameLog gameLog = new HumanReadableGameLog("..\\..\\Results\\GameLog." + gameCount ) )
                {
                    // swap order every other game
                    if (gameCount % 2 == 1)
                    {
                        var temp = player1;
                        player1 = player2;
                        player2 = temp;
                    }

                    var gameConfig = new GameConfig(GetCardSet(player1, player2));

                    GameState gameState = new GameState(
                        gameLog,
                        new PlayerAction[] { player1, player2 },
                        gameConfig);

                    gameState.PlayGameToEnd();

                    PlayerState[] winners = gameState.WinningPlayers;

                    if (winners.Length == 1)
                    {
                        int winningPlayerIndex = ((PlayerAction)winners[0].Actions).playerIndex - 1;
                        winnerCount[winningPlayerIndex]++;
                    }
                    else
                    {
                        tieCount++;
                    }
                }
            }

            for (int index = 0; index < winnerCount.Length; ++index)
            {
                System.Console.WriteLine("Player {0} won: {1} percent of the time.", index + 1, winnerCount[index] / (double)numberOfGames * 100);
            }
            if (tieCount > 0)
            {
                System.Console.WriteLine("Ties: {0} percent of the time.", tieCount / (double)numberOfGames * 100);
            }
        }


        static CardPickByPriority SimplePurchaseOrder3()
        {
            return new CardPickByPriority(
                       CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                       CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                       CardAcceptance.For<CardTypes.Gold>(),
                       CardAcceptance.For<CardTypes.Silver>());

        }

        static CardPickByPriority SimplePurchaseOrder2()
        {
            return new CardPickByPriority(
                       CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                       CardAcceptance.For<CardTypes.Gold>(),
                       CardAcceptance.For<CardTypes.Silver>());
        }

        static CardPickByPriority SimplePurchaseOrder()
        {
            return new CardPickByPriority(
                CardAcceptance.For<CardTypes.Province>(),
                CardAcceptance.For<CardTypes.Gold>(),
                CardAcceptance.For<CardTypes.Duchy>(),
                CardAcceptance.For<CardTypes.Silver>(),
                CardAcceptance.For<CardTypes.Estate>());
        }

        static Card[] GetCardSet(PlayerAction playerAction1, PlayerAction playerAction2)
        {
            var cards = new HashSet<Card>(new CompareCardByType());            
            foreach (Card card in playerAction1.actionOrder.GetNeededCards())
            {
                cards.Add(card);
            }

            foreach (Card card in playerAction2.actionOrder.GetNeededCards())
            {
                cards.Add(card);
            }

            return cards.ToArray();
        }

        struct CompareCardByType
            : IEqualityComparer<Card>
        {
            public bool Equals(Card x, Card y)
            {
                return x.GetType().Equals(y.GetType());
            }

            public int GetHashCode(Card x)
            {
                return x.GetType().GetHashCode();
            }
        }
    }

    public struct CardAcceptance
    {
        internal Card card;
        internal GameStatePredicate match;

        public CardAcceptance(Card card)
        {
            this.card = card;
            this.match = gameState => true;
        }

        public CardAcceptance(Card card, GameStatePredicate match)
        {
            this.card = card;
            this.match = match;
        }

        public static CardAcceptance For<T>()
            where T : Card, new()
        {
            return new CardAcceptance(new T());
        }

        public static CardAcceptance For<T>(GameStatePredicate match)
            where T : Card, new()
        {
            return new CardAcceptance(new T(), match);
        }
    }

    public interface IGetMatchingCard        
    {
        Type GetMatchingCard(GameState gameState, CardPredicate cardPredicate);
        IEnumerable<Card> GetNeededCards();        
    }

    public class CardPickByPriority
        : IGetMatchingCard
    {
        private readonly CardAcceptance[] cardAcceptances;

        public CardPickByPriority(params CardAcceptance[] cardAcceptances)
        {
            this.cardAcceptances = cardAcceptances;
        }

        public Type GetMatchingCard(GameState gameState, CardPredicate cardPredicate)
        {
            foreach (CardAcceptance acceptance in this.cardAcceptances)
            {
                if (cardPredicate(acceptance.card) &&
                    acceptance.match(gameState))
                {
                    return acceptance.card.GetType();
                }
            }

            return null;
        }

        public IEnumerable<Card> GetNeededCards()
        {
            return this.cardAcceptances.Select( cardAcceptance => cardAcceptance.card);
        }        
    }

    public class CardPickByBuildOrder
        : IGetMatchingCard
    {        
        private readonly Card[] buildOrder;

        public CardPickByBuildOrder(params Card[] buildOrer)
        {
            this.buildOrder = buildOrer;
        }

        public Type GetMatchingCard(GameState gameState, CardPredicate cardPredicate)
        {
            var existingCards = new BagOfCards();

            foreach (Card card in gameState.players.CurrentPlayer.AllOwnedCards)
            {
                existingCards.AddCard(card);
            }

            int numberOfTries = 2;
            
            for (int index = 0; index < this.buildOrder.Length; ++index)
            {
                Card currentCard = this.buildOrder[index];

                if (existingCards.HasCard(currentCard))
                {
                    existingCards.RemoveCard(currentCard);
                    continue;
                }
                numberOfTries--;
                
                if (cardPredicate(currentCard))
                {                    
                    return currentCard.GetType();
                }

                if (numberOfTries == 0)
                {
                    break;
                }
            }

            return null;
        }

        public IEnumerable<Card> GetNeededCards()
        {
            return this.buildOrder;
        }        
    }

    public class CardPickConcatenator
        : IGetMatchingCard
    {
        private readonly IGetMatchingCard[] matchers;

        public CardPickConcatenator(params IGetMatchingCard[] matchers)
        {
            this.matchers = matchers;
        }

        public Type GetMatchingCard(GameState gameState, CardPredicate cardPredicate)
        {
            foreach (IGetMatchingCard matcher in this.matchers)
            {
                Type result = matcher.GetMatchingCard(gameState, cardPredicate);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public IEnumerable<Card> GetNeededCards()
        {
            foreach (IGetMatchingCard matcher in this.matchers)
            {
                foreach (Card card in matcher.GetNeededCards())
                {
                    yield return card;
                }
            }
        }        
    }

    class PlayerAction
        : DefaultPlayerAction
    {
        internal readonly int playerIndex;
        internal readonly IGetMatchingCard purchaseOrder;
        internal readonly IGetMatchingCard actionOrder;
        internal readonly IGetMatchingCard trashOrder;
        internal readonly IGetMatchingCard treasurePlayOrder;
        internal readonly IGetMatchingCard discardOrder;

        public PlayerAction(int playerIndex,
            IGetMatchingCard purchaseOrder,
            IGetMatchingCard treasurePlayOrder,
            IGetMatchingCard actionOrder,
            IGetMatchingCard discardOrder,
            IGetMatchingCard trashOrder)
        {
            this.playerIndex = playerIndex;
            this.purchaseOrder = purchaseOrder;
            this.actionOrder = actionOrder;
            this.trashOrder = trashOrder;
            this.treasurePlayOrder = treasurePlayOrder;
            this.discardOrder = discardOrder;
        }        

        public override Type GetCardFromSupplyToBuy(GameState gameState)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.purchaseOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.AvailableCoins >= card.CurrentCoinCost(currentPlayer));
        }

        public override Type GetTreasureFromHandToPlay(GameState gameState)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.treasurePlayOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.Hand.HasCard(card.GetType()));
        }

        public override Type GetActionFromHandToPlay(GameState gameState, bool isOptional)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.actionOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.Hand.HasCard(card.GetType()));
        }

        public override Type GetCardFromHandToTrash(GameState gameState, CardPredicate acceptableCard)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.trashOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.Hand.HasCard(card.GetType()) && acceptableCard(card));
        }

        public override Type GetCardFromHandToDiscard(GameState gameState, bool isOptional)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.discardOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.Hand.HasCard(card.GetType()));
        }

        public override Type GetCardFromRevealedCardsToPutOnDeck(GameState gameState)
        {
            var currentPlayer = gameState.players.CurrentPlayer;
            return this.discardOrder.GetMatchingCard(
                gameState,
                card => currentPlayer.CardsBeingRevealed.HasCard(card.GetType()));
        }

        public override string PlayerName
        {
            get
            {
                return "Player" + playerIndex;
            }
        }
    }    

    namespace Strategies
    {
        public static class Default
        {
            public static CardPickByPriority EmptyPickOrder()
            {
                return new CardPickByPriority();
            }

            public static CardPickByPriority TreasurePlayOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Platinum>(),
                    CardAcceptance.For<CardTypes.Gold>(),
                    CardAcceptance.For<CardTypes.Silver>(),
                    CardAcceptance.For<CardTypes.Copper>());
            }
            
            public static CardPickByPriority DefaultDiscardOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Province>(),
                    CardAcceptance.For<CardTypes.Duchy>(),
                    CardAcceptance.For<CardTypes.Estate>(),
                    CardAcceptance.For<CardTypes.Ruin>(),
                    CardAcceptance.For<CardTypes.Copper>(),
                    CardAcceptance.For<CardTypes.Silver>(),                    
                    CardAcceptance.For<CardTypes.Gold>());
            }
        }

        static class DoubleWarehouse
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: DiscardOrder());
            }

            static CardPickByPriority PurchaseOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 5),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 2),
                           CardAcceptance.For<CardTypes.Gold>(),                           
                           CardAcceptance.For<CardTypes.Warehouse>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Warehouse).Count() < 1),
                           CardAcceptance.For<CardTypes.Warehouse>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Silver).Count() > 2 &&
                                                                                gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Warehouse).Count() < 2),
                           CardAcceptance.For<CardTypes.Silver>());

            }

            static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Warehouse>());
            }

            static CardPickByPriority DiscardOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Province>(),
                    CardAcceptance.For<CardTypes.Duchy>(),
                    CardAcceptance.For<CardTypes.Estate>(),                    
                    CardAcceptance.For<CardTypes.Copper>(),
                    CardAcceptance.For<CardTypes.Silver>(),
                    CardAcceptance.For<CardTypes.Warehouse>(),
                    CardAcceptance.For<CardTypes.Gold>());
            }
        }

        static class DoubleWarehouse2
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: DiscardOrder());
            }

            static IGetMatchingCard PurchaseOrder()
            {
                var highPriority = new CardPickByPriority(
                         CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                         CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 5),
                         CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 2),
                         CardAcceptance.For<CardTypes.Gold>());

                var buildOrder = new CardPickByBuildOrder(
                    new CardTypes.Silver(),
                    new CardTypes.Warehouse(),
                    new CardTypes.Silver(),                    
                    new CardTypes.Silver(),
                    new CardTypes.Warehouse());

                var lowPriority = new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Silver>());

                return new CardPickConcatenator(highPriority, buildOrder, lowPriority);
            }

            static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Warehouse>());
            }

            static CardPickByPriority DiscardOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Province>(),
                    CardAcceptance.For<CardTypes.Duchy>(),
                    CardAcceptance.For<CardTypes.Estate>(),
                    CardAcceptance.For<CardTypes.Copper>(),
                    CardAcceptance.For<CardTypes.Silver>(),
                    CardAcceptance.For<CardTypes.Warehouse>(),
                    CardAcceptance.For<CardTypes.Gold>());
            }
        }

        static class DeathCartDoubleWarehouse
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: TrashOrder(),
                            discardOrder: DiscardOrder());
            }

            static IGetMatchingCard PurchaseOrder()
            {
                var highPriority = new CardPickByPriority(
                          CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 4),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 3));

                var buildOrder = new CardPickByBuildOrder(
                    new CardTypes.DeathCart(),
                    new CardTypes.Silver(),
                    new CardTypes.Warehouse(),
                    new CardTypes.Warehouse());

                var lowPriority = new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Silver>());

                return new CardPickConcatenator(highPriority, buildOrder, lowPriority);                
            }

            static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Warehouse>(gameState => !HasNoRuinsInDeckAndDeathCartInHand(gameState)),
                    CardAcceptance.For<CardTypes.DeathCart>(HasActionInHandOtherThanDeathCart),
                    CardAcceptance.For<CardTypes.DeathCart>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),                    
                    CardAcceptance.For<CardTypes.AbandonedMine>(),
                    CardAcceptance.For<CardTypes.RuinedLibrary>(),
                    CardAcceptance.For<CardTypes.Survivors>(),
                    CardAcceptance.For<CardTypes.RuinedMarket>(),
                    CardAcceptance.For<CardTypes.RuinedVillage>());
            }        

            static CardPickByPriority DiscardOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.Province>(),
                    CardAcceptance.For<CardTypes.Duchy>(),
                    CardAcceptance.For<CardTypes.Estate>(),
                    CardAcceptance.For<CardTypes.RuinedVillage>(ShouldDiscardRuin),
                    CardAcceptance.For<CardTypes.Survivors>(ShouldDiscardRuin),
                    CardAcceptance.For<CardTypes.RuinedMarket>(ShouldDiscardRuin),
                    CardAcceptance.For<CardTypes.RuinedLibrary>(ShouldDiscardRuin),
                    CardAcceptance.For<CardTypes.AbandonedMine>(ShouldDiscardRuin),
                    CardAcceptance.For<CardTypes.Copper>(),
                    CardAcceptance.For<CardTypes.Silver>(),
                    CardAcceptance.For<CardTypes.Warehouse>(),
                    CardAcceptance.For<CardTypes.DeathCart>(),
                    CardAcceptance.For<CardTypes.Gold>());
            }

            static bool ShouldDiscardRuin(GameState gameState)
            {
                return !gameState.players.CurrentPlayer.Hand.HasCard<CardTypes.DeathCart>() ||
                       gameState.players.CurrentPlayer.Hand.Where(card => card.isRuin).Count() > 1;
            }

            static bool HasActionInHandOtherThanDeathCart(GameState gameState)
            {
                return gameState.players.CurrentPlayer.Hand.Where(card => card.isAction && !(card.Is<CardTypes.DeathCart>())).Any();
            }

            static bool HasNoRuinsInDeck(GameState gameState)
            {
                return !gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card.isRuin).Any();
            }

            static bool HasNoRuinsInDeckAndDeathCartInHand(GameState gameState)
            {
                return HasNoRuinsInDeck(gameState) && gameState.players.CurrentPlayer.Hand.HasCard<CardTypes.DeathCart>();
            }

            public static CardPickByPriority TrashOrder()
            {
                return new CardPickByPriority(
                    CardAcceptance.For<CardTypes.RuinedVillage>(),
                    CardAcceptance.For<CardTypes.Survivors>(),
                    CardAcceptance.For<CardTypes.RuinedMarket>(),
                    CardAcceptance.For<CardTypes.RuinedLibrary>(),
                    CardAcceptance.For<CardTypes.AbandonedMine>(),
                    CardAcceptance.For<CardTypes.Warehouse>(HasNoRuinsInDeck));
            }

        }

        static class BigMoney
        {            
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: Default.EmptyPickOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: Default.EmptyPickOrder());
            }

            private static CardPickByPriority PurchaseOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 4),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() <= 3),                           
                           CardAcceptance.For<CardTypes.Silver>());
            }            
        }

        
        static class BigMoneyDoubleSmithy
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber, int secondSmithy=15)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(secondSmithy),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: Default.EmptyPickOrder());
            }

            private static CardPickByPriority PurchaseOrder(int secondSmithy)
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                           CardAcceptance.For<CardTypes.Smithy>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Smithy).Count() < 1),
                           CardAcceptance.For<CardTypes.Smithy>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Smithy).Count() < 2 &&
                                                                             gameState.players.CurrentPlayer.AllOwnedCards.Count() >= secondSmithy),
                           CardAcceptance.For<CardTypes.Silver>());

            }

            private static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Smithy>());
            }
        }

        static class BigMoneySingleSmithy
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: Default.EmptyPickOrder());
            }

            private static CardPickByPriority PurchaseOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                           CardAcceptance.For<CardTypes.Smithy>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Smithy).Count() < 1),                          
                           CardAcceptance.For<CardTypes.Silver>());

            }

            private static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Smithy>());
            }
        }

        static class Test
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new PlayerAction(playerNumber,
                            purchaseOrder: PurchaseOrder(),
                            treasurePlayOrder: Default.TreasurePlayOrder(),
                            actionOrder: ActionOrder(),
                            trashOrder: Default.EmptyPickOrder(),
                            discardOrder: Default.EmptyPickOrder());
            }

            private static IGetMatchingCard PurchaseOrder()
            {
                var highPriority = new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Province>(gameState => gameState.players.CurrentPlayer.AllOwnedCards.Where(card => card is CardTypes.Gold).Count() > 2),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => gameState.GetPile<CardTypes.Province>().Count() < 4));

                var buildOrder = new CardPickByBuildOrder(
                    new CardTypes.Smithy(),
                    new CardTypes.Silver(),
                    new CardTypes.Silver(),
                    new CardTypes.Silver(),
                    new CardTypes.Smithy());

                var lowPriority = new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Silver>());

                return new CardPickConcatenator(highPriority, buildOrder, lowPriority);
            }

            private static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Smithy>());
            }
        }
    }  
}