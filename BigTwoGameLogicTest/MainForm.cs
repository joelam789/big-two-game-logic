using BigTwoGameLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigTwoGameLogicTest
{
    public partial class MainForm : Form
    {
        readonly int BASE_POS_Y = 370;
        readonly int POP_POS_Y = 350;

        BigTwoGame m_Game = new BigTwoGame();

        public MainForm()
        {
            InitializeComponent();
        }

        private string CardToString(Card card)
        {
            switch(card.Suit)
            {
                case 'S': return card.Rank + "♠";
                case 'H': return card.Rank + "♥";
                case 'C': return card.Rank + "♣";
                case 'D': return card.Rank + "♦";
            }
            return card.ToString();
        }

        private string HandToString(List<Card> cards)
        {
            var str = "";
            foreach (var card in cards)
            {
                if (str.Length <= 0) str = CardToString(card);
                else str = str + "," + CardToString(card);
            }
            return str;
        }

        private void LoadCardImages(List<Card> cards)
        {
            List<PictureBox> cardBoxes = new List<PictureBox>()
            { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
                pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10,
                pictureBox11, pictureBox12, pictureBox13
            };

            int maxCount = cardBoxes.Count;
            var imgDir = AppDomain.CurrentDomain.BaseDirectory;

            if (cards == null)
            {
                for (int i = 0; i < maxCount; i++)
                {
                    var fileName = "BG";
                    cardBoxes[i].Load(imgDir + "/Image/" + fileName + ".png");
                    cardBoxes[i].Visible = true;
                    cardBoxes[i].Top = BASE_POS_Y;
                }
                return;
            }
            else
            {
                if (cards.Count < maxCount) maxCount = cards.Count;

                for (int i = 0; i < maxCount; i++)
                {
                    var fileName = cards[i].ToString();
                    cardBoxes[i].Load(imgDir + "/Image/" + fileName + ".png");
                    cardBoxes[i].Visible = true;
                    cardBoxes[i].Top = BASE_POS_Y;
                }

                for (int i = maxCount; i < cardBoxes.Count; i++)
                {
                    cardBoxes[i].Visible = false;
                    cardBoxes[i].Top = BASE_POS_Y;
                }
            }

            
        }

        public void LogMsg(string msg)
        {
            BeginInvoke((Action)(() =>
            {
                if (mmLogger.Lines.Length > 1024)
                {
                    List<string> finalLines = mmLogger.Lines.ToList();
                    finalLines.RemoveRange(0, 512);
                    mmLogger.Lines = finalLines.ToArray();
                }

                mmLogger.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n");
                mmLogger.SelectionStart = mmLogger.Text.Length;
                mmLogger.ScrollToCaret();
            }));
        }

        private bool AutoPlay()
        {
            var lastPlay = m_Game.GetLastPlay();
            var lastPlayerName = m_Game.GetLastPlayer();
            var currentPlayerName = m_Game.GetCurrentPlayer();

            if (string.IsNullOrEmpty(currentPlayerName) || currentPlayerName == "You") return false;

            var currentPlayer = m_Game.FindPlayer(currentPlayerName);
            if (currentPlayer == null) return false;

            if (string.IsNullOrEmpty(lastPlayerName)) // first play of current round
            {
                var play = BigTwoLogic.TryToGiveOutBest(currentPlayer.CurrentHand.GetCards(), 0, "3D");
                if (play == null || play.Count <= 0) return false;

                return m_Game.AcceptPlay(currentPlayerName, play);
            }
            else
            {
                if (lastPlayerName == currentPlayerName)
                {
                    var play = BigTwoLogic.TryToGiveOutBest(currentPlayer.CurrentHand.GetCards(), 0);
                    return m_Game.AcceptPlay(currentPlayerName, play);
                }
                else
                {
                    var play = BigTwoLogic.TryToGiveOutBest(currentPlayer.CurrentHand.GetCards(), lastPlay.Count);
                    if (play == null || play.Count <= 0) return m_Game.AcceptPlay(currentPlayerName, new List<int>());
                    else
                    {
                        var playCards = currentPlayer.CurrentHand.GetCards(play);
                        if (BigTwoLogic.CheckBetterCards(playCards, lastPlay)) return m_Game.AcceptPlay(currentPlayerName, play);
                        else return m_Game.AcceptPlay(currentPlayerName, new List<int>());
                    }
                }
            }
        }

        private void UpdateGameDisplay()
        {
            btnPlayerA.Enabled = false;
            btnPlayerB.Enabled = false;
            btnPlayerC.Enabled = false;
            btnPlay.Enabled = false;

            var currentPlayer = m_Game.GetCurrentPlayer();
            var lastPlayer = m_Game.GetLastPlayer();
            var players = m_Game.GetPlayers();
            foreach (var player in players)
            {
                if (player.PlayerName == "You")
                {
                    if (player.CurrentHand == null) LoadCardImages(null);
                    else LoadCardImages(player.CurrentHand.GetCards());
                    btnPlay.Enabled = (currentPlayer == player.PlayerName);
                    btnPass.Enabled = lastPlayer == currentPlayer ? false : btnPlay.Enabled;
                    btnGive.Enabled = btnPlay.Enabled || btnPass.Enabled;
                }
                else if (player.PlayerName == "PlayerA")
                {
                    lblPlayerA.Text = "[ ? ]";
                    if (player.CurrentHand != null)
                    {
                        lblPlayerA.Text = "[ " + player.CurrentHand.GetNumberOfCards() + " ]";
                        var cards = HandToString(player.CurrentHand.GetCards());
                        lbHandA.Items.Clear();
                        lbHandA.Items.AddRange(cards.Split(','));
                    }
                    btnPlayerA.Enabled = (currentPlayer == player.PlayerName);
                }
                else if (player.PlayerName == "PlayerB")
                {
                    lblPlayerB.Text = "[ ? ]";
                    if (player.CurrentHand != null)
                    {
                        lblPlayerB.Text = "[ " + player.CurrentHand.GetNumberOfCards() + " ]";
                        var cards = HandToString(player.CurrentHand.GetCards());
                        edtHandB.Text = cards;
                    }
                    btnPlayerB.Enabled = (currentPlayer == player.PlayerName);
                }
                else if (player.PlayerName == "PlayerC")
                {
                    lblPlayerC.Text = "[ ? ]";
                    if (player.CurrentHand != null)
                    {
                        lblPlayerC.Text = "[ " + player.CurrentHand.GetNumberOfCards() + " ]";
                        var cards = HandToString(player.CurrentHand.GetCards());
                        lbHandC.Items.Clear();
                        lbHandC.Items.AddRange(cards.Split(','));
                    }
                    btnPlayerC.Enabled = (currentPlayer == player.PlayerName);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            mmLogger.ReadOnly = true;

            lbHandA.Items.Clear();
            lbHandC.Items.Clear();
            edtHandB.Text = "";

            LoadCardImages(null);

            m_Game.GetGameReady();

            m_Game.JoinGame("You");
            m_Game.JoinGame("PlayerA");
            m_Game.JoinGame("PlayerB");
            m_Game.JoinGame("PlayerC");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var okay = m_Game.StartNewRound();

            UpdateGameDisplay();

            if (m_Game.GetGameState() == "PlayingCards")
            {
                btnStart.Enabled = false;
                if (okay)
                {
                    LogMsg("===== START =====");
                    LogMsg("Game Round - " + m_Game.GetShoeCode() + "-" + m_Game.GetRoundIndex());
                }
            }
        }

        private void btnGive_Click(object sender, EventArgs e)
        {
            List<PictureBox> cardBoxes = new List<PictureBox>()
            { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
                pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10,
                pictureBox11, pictureBox12, pictureBox13
            };

            if (m_Game.GetGameState() != "PlayingCards") return;

            var lastPlay = m_Game.GetLastPlay();
            var lastPlayerName = m_Game.GetLastPlayer();
            var currentPlayerName = m_Game.GetCurrentPlayer();

            if (currentPlayerName != "You") return;

            var player = m_Game.FindPlayer("You");

            if (string.IsNullOrEmpty(lastPlayerName) || lastPlay == null || lastPlay.Count <= 0)
            {
                var items = BigTwoLogic.TryToGiveOutBest(player.CurrentHand.GetCards(), 0, "3D");
                if (items == null || items.Count <= 0)
                {
                    LogMsg("No tips found");
                    return;
                }
                else for (var i = 0; i < cardBoxes.Count; i++)
                {
                    if (items.IndexOf(i) >= 0) cardBoxes[i].Top = POP_POS_Y;
                    else cardBoxes[i].Top = BASE_POS_Y;
                }
            }
            else
            {
                var items = BigTwoLogic.TryToGiveOutBest(player.CurrentHand.GetCards(), 
                    lastPlayerName == "You" ? 0 : lastPlay.Count);

                if (items == null || items.Count <= 0)
                {
                    LogMsg("You have to pass");
                    return;
                }
                else
                {
                    if (lastPlayerName == "You")
                    {
                        for (var i = 0; i < cardBoxes.Count; i++)
                        {
                            if (items.IndexOf(i) >= 0) cardBoxes[i].Top = POP_POS_Y;
                            else cardBoxes[i].Top = BASE_POS_Y;
                        }
                    }
                    else
                    {
                        var playCards = player.CurrentHand.GetCards(items);
                        if (BigTwoLogic.CheckBetterCards(playCards, lastPlay))
                        {
                            for (var i = 0; i < cardBoxes.Count; i++)
                            {
                                if (items.IndexOf(i) >= 0) cardBoxes[i].Top = POP_POS_Y;
                                else cardBoxes[i].Top = BASE_POS_Y;
                            }
                        }
                        else
                        {
                            LogMsg("You have to pass");
                            return;
                        }
                    }
                }
            }
        }

        private void btnPlayerA_Click(object sender, EventArgs e)
        {
            var currentName = m_Game.GetCurrentPlayer();
            var lastName = m_Game.GetLastPlayer();
            if (AutoPlay())
            {
                var currentPlayerName = m_Game.GetCurrentPlayer();
                var lastPlayerName = m_Game.GetLastPlayer();
                var lastPlay = m_Game.GetLastPlay();

                if (m_Game.GetLastAccepted().Count <= 0)
                {
                    LogMsg(currentName + " => PASS ");
                }
                else
                {
                    LogMsg(lastPlayerName + " => " + HandToString(lastPlay));
                }

                UpdateGameDisplay();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            //LogMsg((sender as Control).Name);

            if (!btnPass.Enabled && !btnPlay.Enabled) return;

            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            if (pb.Top == BASE_POS_Y) pb.Top = POP_POS_Y;
            else if (pb.Top == POP_POS_Y) pb.Top = BASE_POS_Y;
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            if (m_Game.GetGameState() != "PlayingCards") return;

            var currentPlayerName = m_Game.GetCurrentPlayer();

            if (currentPlayerName != "You") return;

            if (m_Game.AcceptPlay(currentPlayerName, new List<int>()))
            {
                LogMsg(currentPlayerName + " => PASS ");

                List<PictureBox> cardBoxes = new List<PictureBox>()
                { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
                    pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10,
                    pictureBox11, pictureBox12, pictureBox13
                };

                for (var i = 0; i < cardBoxes.Count; i++)
                {
                    cardBoxes[i].Top = BASE_POS_Y;
                }

                UpdateGameDisplay();
            }
            else
            {
                LogMsg("You are not allowed to PASS");
            }

        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (m_Game.GetGameState() != "PlayingCards") return;

            var lastPlay = m_Game.GetLastPlay();
            var lastPlayerName = m_Game.GetLastPlayer();
            var currentPlayerName = m_Game.GetCurrentPlayer();

            if (currentPlayerName != "You") return;

            List<PictureBox> cardBoxes = new List<PictureBox>()
            { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
                pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10,
                pictureBox11, pictureBox12, pictureBox13
            };

            var play = new List<int>();
            var player = m_Game.FindPlayer("You");

            for (var i = 0; i < cardBoxes.Count; i++)
            {
                if (!cardBoxes[i].Visible) continue;
                if (cardBoxes[i].Top == POP_POS_Y) play.Add(i);
            }

            if (play == null || play.Count <= 0)
            {
                LogMsg("Please select cards to play or click PASS to end your turn");
                return;
            }

            var playCards = player.CurrentHand.GetCards(play);

            if (m_Game.AcceptPlay(currentPlayerName, play))
            {
                LogMsg(currentPlayerName + " => " + HandToString(playCards));
                UpdateGameDisplay();
            }
            else
            {
                LogMsg("You cannot play like that");
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            if (m_Game.GetGameState() == "EndRound")
            {
                m_Game.CalculateScores();

                var players = m_Game.GetPlayers();
                foreach (var player in players)
                {
                    LogMsg(player.PlayerName + " : " + player.GameScore);
                }

                m_Game.GetGameReady();

                btnCalculate.Enabled = false;
                btnStart.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_Game == null) return;

            if (m_Game.GetGameState() == "EndRound")
            {
                btnCalculate.Enabled = true;
                return;
            }

            if (m_Game.GetGameState() != "PlayingCards") return;
            
            if (rbManualPlay.Checked) return;
            else if (rbNpcAutoPlay.Checked)
            {
                if (m_Game.GetCurrentPlayer() == "You") return;

                if (btnPlayerA.Enabled) btnPlayerA.PerformClick();
                else if (btnPlayerB.Enabled) btnPlayerB.PerformClick();
                else if (btnPlayerC.Enabled) btnPlayerC.PerformClick();

            }
            else if (rbFullAutoPlay.Checked)
            {
                if (btnPlayerA.Enabled) btnPlayerA.PerformClick();
                else if (btnPlayerB.Enabled) btnPlayerB.PerformClick();
                else if (btnPlayerC.Enabled) btnPlayerC.PerformClick();
                else if (btnPass.Enabled || btnPlay.Enabled)
                {
                    bool foundTips = false;
                    btnGive.PerformClick();

                    List<PictureBox> cardBoxes = new List<PictureBox>()
                    { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5,
                        pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10,
                        pictureBox11, pictureBox12, pictureBox13
                    };

                    for (var i = 0; i < cardBoxes.Count; i++)
                    {
                        if (cardBoxes[i].Top == POP_POS_Y)
                        {
                            foundTips = true;
                            break;
                        }
                    }

                    if (foundTips) btnPlay.PerformClick();
                    else btnPass.PerformClick();
                }
            }
            
        }

        
    }
}
