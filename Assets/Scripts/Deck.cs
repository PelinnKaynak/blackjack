using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public GameObject dealer;
    public GameObject player;

    public Button hitButton;
    public Button stickButton;
    public Button playButton;

    public TMP_Dropdown betDropdown;

    public Text finalMessage;
    public Text probMessage;
    public TMP_Text creditText;
    public TMP_Text dealerPointsText;
    public TMP_Text playerPointsText;

    public int[] values = new int[52];
    int cardIndex = 0;

    private bool gameOver = false;
    private bool dealerCardShown = false;

    private int bank = 1000;
    private int currentBet = 10;

    private void Awake()
    {
        InitCardValues();
    }

    private void Start()
    {
        UpdateCreditUI();
        UpdatePointsUI();

        hitButton.interactable = false;
        stickButton.interactable = false;

        if (playButton != null)
            playButton.interactable = true;

        if (betDropdown != null)
            betDropdown.interactable = true;

        finalMessage.text = "";
        probMessage.text = "";
    }

    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            int rank = i % 13;

            if (rank == 0)
                values[i] = 11;
            else if (rank >= 10)
                values[i] = 10;
            else
                values[i] = rank + 1;
        }
    }

    private void ShuffleCards()
    {
        for (int i = 0; i < faces.Length; i++)
        {
            int randomIndex = Random.Range(i, faces.Length);

            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;

            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }

        cardIndex = 0;
    }

    public void Play()
    {
        if (bank < 10)
        {
            finalMessage.text = "No tienes crédito suficiente.";
            return;
        }

        currentBet = GetSelectedBet();

        if (currentBet % 10 != 0)
        {
            finalMessage.text = "La apuesta debe ser múltiplo de 10.";
            return;
        }

        if (currentBet > bank)
        {
            finalMessage.text = "No tienes suficiente crédito.";
            return;
        }

        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();

        cardIndex = 0;
        gameOver = false;
        dealerCardShown = false;

        ShuffleCards();
        StartGame();
    }

    void StartGame()
    {
        gameOver = false;
        dealerCardShown = false;

        hitButton.interactable = true;
        stickButton.interactable = true;

        if (playButton != null)
            playButton.interactable = false;

        if (betDropdown != null)
            betDropdown.interactable = false;

        finalMessage.text = "";
        probMessage.text = "";

        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        UpdatePointsUI();

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        if (playerPoints == 21 || dealerPoints == 21)
        {
            RevealDealerCard();

            if (playerPoints == 21 && dealerPoints == 21)
            {
                ApplyDraw();
                EndGame("Empate. Ambos tienen Blackjack.");
            }
            else if (playerPoints == 21)
            {
                ApplyWin();
                EndGame("Blackjack del jugador. Has ganado.");
            }
            else
            {
                ApplyLoss();
                EndGame("Blackjack del dealer. Has perdido.");
            }
        }
        else
        {
            CalculateProbabilities();
        }
    }

    private void CalculateProbabilities()
    {
        if (gameOver)
            return;

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        int remainingCards = values.Length - cardIndex;

        if (remainingCards <= 0)
        {
            probMessage.text = "No quedan cartas.";
            return;
        }

        int dealerBetterCount = 0;
        int player17to21Count = 0;
        int playerBustCount = 0;

        for (int i = cardIndex; i < values.Length; i++)
        {
            int nextCard = values[i];

            int simulatedPlayer = SimulateScore(playerPoints, nextCard);
            int simulatedDealer = SimulateScore(dealerPoints, nextCard);

            if (simulatedDealer > playerPoints && simulatedDealer <= 21)
                dealerBetterCount++;

            if (simulatedPlayer >= 17 && simulatedPlayer <= 21)
                player17to21Count++;

            if (simulatedPlayer > 21)
                playerBustCount++;
        }

        float probDealerBetter = (float)dealerBetterCount / remainingCards;
        float prob17to21 = (float)player17to21Count / remainingCards;
        float probBust = (float)playerBustCount / remainingCards;

        probMessage.text =
            "Deal > Play: " + probDealerBetter.ToString("F4") + "\n" +
            "17<=X<=21: " + prob17to21.ToString("F4") + "\n" +
            "X > 21: " + probBust.ToString("F4");
    }

    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdatePointsUI();
    }

    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdatePointsUI();
        CalculateProbabilities();
    }

    public void Hit()
    {
        if (gameOver)
            return;

        RevealDealerCard();
        PushPlayer();

        int playerPoints = player.GetComponent<CardHand>().points;

        if (playerPoints > 21)
        {
            ApplyLoss();
            EndGame("El jugador se pasa de 21. Has perdido.");
            return;
        }

        if (playerPoints == 21)
        {
            Stand();
            return;
        }

        CalculateProbabilities();
    }

    public void Stand()
    {
        if (gameOver)
            return;

        RevealDealerCard();

        while (dealer.GetComponent<CardHand>().points <= 16)
        {
            PushDealer();
        }

        int dealerPoints = dealer.GetComponent<CardHand>().points;
        int playerPoints = player.GetComponent<CardHand>().points;

        if (dealerPoints > 21)
        {
            ApplyWin();
            EndGame("El dealer se pasa de 21. Has ganado.");
        }
        else if (dealerPoints > playerPoints)
        {
            ApplyLoss();
            EndGame("El dealer gana.");
        }
        else if (dealerPoints < playerPoints)
        {
            ApplyWin();
            EndGame("El jugador gana.");
        }
        else
        {
            ApplyDraw();
            EndGame("Empate.");
        }
    }

    private void EndGame(string message)
    {
        gameOver = true;
        finalMessage.text = message;

        hitButton.interactable = false;
        stickButton.interactable = false;

        if (playButton != null)
            playButton.interactable = bank >= 10;

        if (betDropdown != null)
            betDropdown.interactable = bank >= 10;

        UpdatePointsUI();
        UpdateCreditUI();
    }

    private void RevealDealerCard()
    {
        if (!dealerCardShown)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            dealerCardShown = true;
        }
    }

    private int SimulateScore(int currentPoints, int nextCardValue)
    {
        int total = currentPoints + nextCardValue;

        if (nextCardValue == 11 && total > 21)
            total -= 10;

        return total;
    }

    private void UpdateCreditUI()
    {
        if (creditText != null)
            creditText.text = bank.ToString();
    }

    private void UpdatePointsUI()
    {
        if (dealerPointsText != null)
            dealerPointsText.text = dealer.GetComponent<CardHand>().points.ToString();

        if (playerPointsText != null)
            playerPointsText.text = player.GetComponent<CardHand>().points.ToString();
    }

    private int GetSelectedBet()
    {
        if (betDropdown == null)
            return 10;

        string txt = betDropdown.options[betDropdown.value].text;

        string digits = "";
        foreach (char c in txt)
        {
            if (char.IsDigit(c))
                digits += c;
        }

        if (digits == "")
            return 10;

        return int.Parse(digits);
    }

    private void ApplyWin()
    {
        bank += currentBet * 2;
        UpdateCreditUI();
    }

    private void ApplyLoss()
    {
        bank -= currentBet;
        if (bank < 0)
            bank = 0;

        UpdateCreditUI();
    }

    private void ApplyDraw()
    {
        UpdateCreditUI();
    }
}