﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigTwoGameLogic
{
    public class BigTwoGame
    {
        public enum GAME_STATUS
        {
            Unknown = 0,
            WaitToStart,      // 1
            PlayingCards,     // 2
            EndRound          // 3
        };

        private List<string> m_GameStatus = new List<string>
        {
            "Unknown",
            "WaitToStart",
            "PlayingCards",
            "EndRound"
        };

        private GAME_STATUS m_GameState = GAME_STATUS.Unknown;

        private string m_ShoeCode = "";
        private int m_RoundIndex = 0;

        private List<Player> m_Players = new List<Player>();

        private Deck m_Deck = new Deck();

        private List<Card> m_LastPlay = new List<Card>();
        private List<Card> m_LastAccepted = new List<Card>();
        private string m_LastPlayer = "";
        private string m_CurrentPlayer = "";

        public bool GetGameReady(bool withNewShoe = false)
        {
            if (m_GameState == GAME_STATUS.Unknown 
                || m_GameState == GAME_STATUS.EndRound)
            {
                m_GameState = GAME_STATUS.WaitToStart;

                if (withNewShoe || string.IsNullOrEmpty(m_ShoeCode))
                {
                    m_ShoeCode = DateTime.Now.ToString("yyyyMMddHHmmss");
                    m_RoundIndex = 0;
                }

                return true;
            }
            return m_GameState == GAME_STATUS.WaitToStart;
        }

        public List<Player> GetPlayers()
        {
            var result = new List<Player>();
            result.AddRange(m_Players);
            return result;
        }

        public Player FindPlayer(string playerName)
        {
            foreach (var player in m_Players)
            {
                if (player.PlayerName == playerName) return player;
            }
            return null;
        }

        public string GetShoeCode()
        {
            return string.Copy(m_ShoeCode);
        }

        public int GetRoundIndex()
        {
            return m_RoundIndex;
        }

        public string GetGameState()
        {
            return m_GameStatus[(int)m_GameState];
        }

        public string GetCurrentPlayer()
        {
            return string.Copy(m_CurrentPlayer);
        }

        public string GetLastPlayer()
        {
            return string.Copy(m_LastPlayer);
        }

        public List<Card> GetLastPlay()
        {
            var result = new List<Card>();
            result.AddRange(m_LastPlay);
            return result;
        }

        public List<Card> GetLastAccepted()
        {
            var result = new List<Card>();
            result.AddRange(m_LastAccepted);
            return result;
        }

        public bool JoinGame(string playerName)
        {
            if (m_GameState != GAME_STATUS.WaitToStart) return false;

            if (m_Players.Count >= 4) return false;

            foreach (var player in m_Players)
            {
                if (player.PlayerName == playerName) return false;
            }

            m_Players.Add(new Player(playerName));

            return true;
        }

        public bool LeaveGame(string playerName)
        {
            if (m_GameState != GAME_STATUS.WaitToStart) return false;

            if (m_Players.Count <= 0) return false;

            foreach (var player in m_Players)
            {
                if (player.PlayerName == playerName)
                {
                    m_Players.Remove(player);
                    return true;
                }
            }

            return false;
        }

        public bool StartNewRound()
        {
            if (m_GameState != GAME_STATUS.WaitToStart) return false;
            if (m_Players.Count <= 1) return false;

            m_RoundIndex++;

            m_LastPlay.Clear();
            m_LastAccepted.Clear();
            m_LastPlayer = "";
            m_CurrentPlayer = "";

            m_Deck.Shuffle();
            var hands = m_Deck.Distribute(m_Players.Count, "3D");
            for (var i=0; i< m_Players.Count; i++)
            {
                hands[i].SortCards();
                m_Players[i].CurrentHand = hands[i];
                if (hands[i].IndexOfCard("3D") >= 0)
                    m_CurrentPlayer = m_Players[i].PlayerName;
            }

            m_GameState = GAME_STATUS.PlayingCards;
            return true;
        }

        public bool AcceptPlay(string playerName, List<int> cardList)
        {
            if (m_GameState != GAME_STATUS.PlayingCards) return false;
            if (string.IsNullOrEmpty(m_CurrentPlayer)) return false;
            if (m_CurrentPlayer != playerName) return false;

            Player lastPlayer = null;
            Player currentPlayer = null;
            int currentPlayerIndex = -1;
            for (var i = 0; i < m_Players.Count; i++)
            {
                if (m_Players[i].PlayerName == m_CurrentPlayer)
                {
                    currentPlayer = m_Players[i];
                    currentPlayerIndex = i;
                }
                if (!string.IsNullOrEmpty(m_LastPlayer)
                    && m_Players[i].PlayerName == m_LastPlayer)
                {
                    lastPlayer = m_Players[i];
                }
            }

            if (currentPlayer == null) return false;

            var playerCards = currentPlayer.CurrentHand.GetCards();
            List<Card> playCards = new List<Card>();
            foreach (var idx in cardList)
            {
                if (idx < 0 || idx >= playerCards.Count) return false;
                else playCards.Add(playerCards[idx]);
            }

            Hand playHand = playCards.Count > 0 ? new Hand(playCards) : null;
            if (playHand != null) playHand.SortCards();

            List<Card> lastPlay = m_LastPlay == null || m_LastPlay.Count == 0 ? null : new List<Card>();
            if (lastPlay != null && m_LastPlay != null) lastPlay.AddRange(m_LastPlay);

            // if it's first play
            if (lastPlayer == null)
            {
                if (playHand == null) return false; // cannot pass
                if (playHand.IndexOfCard("3D") < 0) return false; // must contain Diamond 3
                if (playCards.Count == 3) return false; // not allow triple as first play

                var playHandCards = playHand.GetCards();
                if (BigTwoLogic.CheckBetterCards(playHandCards, null))
                {
                    m_LastAccepted.Clear();
                    m_LastAccepted.AddRange(playHandCards);

                    m_LastPlay.Clear();
                    m_LastPlay.AddRange(playHandCards);
                    m_LastPlayer = string.Copy(m_CurrentPlayer);

                    foreach (var playCard in playCards)
                        currentPlayer.CurrentHand.Discard(playCard.ToString());

                    currentPlayer.CurrentHand.SortCards();

                    int nextPlayerIndex = (currentPlayerIndex + 1) % m_Players.Count;
                    m_CurrentPlayer = m_Players[nextPlayerIndex].PlayerName;

                    return true;
                }
                else return false;
            }
            else
            {
                int nextPlayerIndex = (currentPlayerIndex + 1) % m_Players.Count;
                var nextPlayer = m_Players[nextPlayerIndex];
                var nextPlayerCards = nextPlayer.CurrentHand.GetCards();

                if (nextPlayerCards.Count == 1)
                {
                    if (cardList.Count == 1 
                        && cardList[0] != playerCards.Count - 1) return false; // must be the biggest one
                }

                if (lastPlayer == currentPlayer)
                {
                    if (playHand == null) return false; // cannot pass

                    var playHandCards = playHand.GetCards();
                    if (BigTwoLogic.CheckBetterCards(playHandCards, null))
                    {
                        m_LastAccepted.Clear();
                        m_LastAccepted.AddRange(playHandCards);

                        m_LastPlay.Clear();
                        m_LastPlay.AddRange(playHandCards);
                        m_LastPlayer = string.Copy(m_CurrentPlayer);

                        foreach (var playCard in playCards)
                            currentPlayer.CurrentHand.Discard(playCard.ToString());

                        if (currentPlayer.CurrentHand.GetNumberOfCards() == 0)
                        {
                            m_CurrentPlayer = "";
                            m_GameState = GAME_STATUS.EndRound;
                        }
                        else
                        {
                            currentPlayer.CurrentHand.SortCards();
                            m_CurrentPlayer = nextPlayer.PlayerName;
                        }

                        return true;
                    }
                    else return false;
                }
                else
                {
                    if (playHand == null) // pass
                    {
                        m_LastAccepted.Clear();
                        m_CurrentPlayer = nextPlayer.PlayerName;
                        return true;
                    }
                    else
                    {
                        var playHandCards = playHand.GetCards();
                        if (BigTwoLogic.CheckBetterCards(playHandCards, lastPlay))
                        {
                            m_LastAccepted.Clear();
                            m_LastAccepted.AddRange(playHandCards);

                            m_LastPlay.Clear();
                            m_LastPlay.AddRange(playHandCards);
                            m_LastPlayer = string.Copy(m_CurrentPlayer);

                            foreach (var playCard in playCards)
                                currentPlayer.CurrentHand.Discard(playCard.ToString());

                            if (currentPlayer.CurrentHand.GetNumberOfCards() == 0)
                            {
                                m_CurrentPlayer = "";
                                m_GameState = GAME_STATUS.EndRound;
                            }
                            else
                            {
                                currentPlayer.CurrentHand.SortCards();
                                m_CurrentPlayer = nextPlayer.PlayerName;
                            }

                            return true;
                        }
                        else return false;
                    }
                }
            }

            //return false;
        }

        public bool CalculateScores()
        {
            if (m_GameState != GAME_STATUS.EndRound) return false;

            int total = 0;
            Player winner = null;
            
            foreach (var player in m_Players)
            {
                int count = player.CurrentHand.GetNumberOfCards();
                if (count == 0) winner = player;
                else
                {
                    if (count >= 13) count = count * 4;
                    else if (count >= 10) count = count * 3;
                    else if (count >= 8) count = count * 2;

                    total += count;
                    player.GameScore = 0;
                }
            }

            if (winner != null) winner.GameScore = total;

            return true;

        }

    }

    public class Card
    {
        public char Rank { get; set; }
        public char Suit { get; set; }

        public Card(char rank, char suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public Card(Card another)
        {
            Rank = another.Rank;
            Suit = another.Suit;
        }

        public override string ToString()
        {
            return Rank.ToString() + Suit;
        }
    }

    public class Hand
    {
        private List<Card> m_Cards = new List<Card>();

        public Hand(List<Card> cards)
        {
            if (cards != null)
            {
                m_Cards.Clear();
                m_Cards.AddRange(cards);
            }
        }

        public List<Card> GetCards()
        {
            var cards = new List<Card>();
            cards.AddRange(m_Cards);
            return cards;
        }

        public List<Card> GetCards(List<int> list)
        {
            var cards = new List<Card>();
            foreach (var idx in list) cards.Add(m_Cards[idx]);
            return cards;
        }

        public void SetCards(List<Card> cards)
        {
            m_Cards.Clear();
            if (cards != null) m_Cards.AddRange(cards);
        }

        public int GetNumberOfCards()
        {
            return m_Cards.Count;
        }

        public bool Discard(int idx)
        {
            if (idx >= 0 && idx < m_Cards.Count)
            {
                m_Cards.RemoveAt(idx);
                return true;
            }
            return false;
        }

        public bool Discard(string card)
        {
            var idx = IndexOfCard(card);
            return Discard(idx);
        }

        public int IndexOfCard(string card)
        {
            int idx = -1;
            for (var i = 0; i < m_Cards.Count; i++)
            {
                if (m_Cards[i].ToString() == card)
                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

        public void SortCards()
        {
            var cards = GetCards();
            SetCards(BigTwoLogic.MergeSort(cards));
        }

    }

    public class Deck
    {
        private List<Card> m_Cards = new List<Card>();

        public Deck()
        {
            Reset();
        }

        public void Reset()
        {
            m_Cards.Clear();
            m_Cards = BigTwoLogic.GenerateDeckCards();
        }

        public void Shuffle()
        {
            m_Cards = BigTwoLogic.ShuffleCards(m_Cards);
        }

        public List<Hand> Distribute(int count = 4, string keyCard = "")
        {
            var hand1 = new Hand(m_Cards.GetRange(0, 13));
            var hand2 = new Hand(m_Cards.GetRange(13, 13));
            var hand3 = new Hand(m_Cards.GetRange(26, 13));
            var hand4 = new Hand(m_Cards.GetRange(39, 13));

            if (count >= 4)
            {
                return new List<Hand>() { hand1, hand2, hand3, hand4 };
            }
            else if (count == 3)
            {
                if (!string.IsNullOrEmpty(keyCard) && hand4.IndexOfCard(keyCard) >= 0)
                {
                    return new List<Hand>() { hand2, hand3, hand4 };
                }
                else
                {
                    return new List<Hand>() { hand1, hand2, hand3 };
                }
            }
            else if (count == 2)
            {
                if (!string.IsNullOrEmpty(keyCard)
                    && (hand3.IndexOfCard(keyCard) >= 0 || hand4.IndexOfCard(keyCard) >= 0))
                {
                    return new List<Hand>() { hand3, hand4 };
                }
                else
                {
                    return new List<Hand>() { hand1, hand2 };
                }
            }
            else if (count == 1)
            {
                if (!string.IsNullOrEmpty(keyCard))
                {
                    if (hand2.IndexOfCard(keyCard) >= 0) return new List<Hand>() { hand2 };
                    else if (hand3.IndexOfCard(keyCard) >= 0) return new List<Hand>() { hand3 };
                    else if (hand4.IndexOfCard(keyCard) >= 0) return new List<Hand>() { hand4 };
                    else return new List<Hand>() { hand1 };
                }
                else
                {
                    return new List<Hand>() { hand1 };
                }
            }

            return new List<Hand>();
        }
    }

    public class Player
    {
        public string PlayerName { get; set; }
        public int GameScore { get; set; }
        public Hand CurrentHand { get; set; }

        public Player(string playerName)
        {
            PlayerName = playerName;
            GameScore = 0;
            CurrentHand = new Hand(new List<Card>());
        }
    }
}