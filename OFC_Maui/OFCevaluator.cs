using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace OFC_Blazor;

public static class OFCevaluator
{
    static void MonteCarloKernel(
        int index,              // The global thread index (1D in this case)
        int numPlays,   // index for where the results will be accumulated
            int maxNumThreads,
            int numThreadSamples,
            int[] p1Cards,
            int[] p2Cards,
            int[] cardsRemaining,
            Random RNG,
            int[] AccumScores, // results for each play
            int[] PlayCounts, // number of times each play was evaluated
            CancellationToken token
        )
    {
        // index for where the results will be accumulated
        //var netScore = 0;

        var deck = new Deck(cardsRemaining, RNG);

        for (int i = 0; i < numThreadSamples; i++)
        {
            if (token.IsCancellationRequested)
                break;
            var resultsIdx = (index + i) % numPlays; // index for where the results will be accumulated
            var playIdx = resultsIdx * 13;


            deck.Init();
            var p1Hand = new Hand(new SingleHand(p1Cards[playIdx..(playIdx + 3)]), new SingleHand(p1Cards[(playIdx + 3)..(playIdx + 3 + 5)]), new SingleHand(p1Cards[(playIdx + 8)..(playIdx + 8 + 5)]));
            var p2Hand = new Hand(new SingleHand(p2Cards[0..(0 + 2)]), new SingleHand(p2Cards[3..(3 + 5)]), new SingleHand(p2Cards[8..(8 + 5)]));

            var game = new Game(p1Hand, p2Hand);

            // Game Loop
            while (!game.hand0.IsHandComplete() || !game.hand1.IsHandComplete())
            {
                // villain play
                if (!game.hand1.IsHandComplete())
                {
                    // player 2 initial
                    if (game.hand1.hand0.Len() == 0 && game.hand1.hand1.Len() == 0 && game.hand1.hand2.Len() == 0)
                    {
                        var cards = deck.Draw5Cards();
                        // error condition
                        if (cards[4] == -1)
                        {
                            return;
                        }
                        while (true)
                        {
                            var playIds = new byte[] {
                                (byte)(deck.GetNextRandom() % 3),
                                (byte)(deck.GetNextRandom() % 3),
                                (byte)(deck.GetNextRandom() % 3),
                                (byte)(deck.GetNextRandom() % 3),
                                (byte)(deck.GetNextRandom() % 3),
                            };
                            if (game.Play(1, cards, playIds))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        var cards = deck.Draw3Cards();
                        // error condition
                        if (cards[2] == -1)
                        {
                            return;
                        }
                        while (true)
                        {
                            var playIds = new byte[] {
                                    (byte)(deck.GetNextRandom() % 4),
                                    (byte)(deck.GetNextRandom() % 4),
                                    (byte)(deck.GetNextRandom() % 4),
                                };
                            if (game.Play(1, cards, playIds))
                                break;
                        }
                    }
                }

                // hero play
                if (!game.hand0.IsHandComplete())
                {
                    var cards = deck.Draw3Cards();
                    // error condition
                    if (cards[2] == -1)
                    {
                        return;
                    }
                    var playIds = new byte[3];
                    while (true)
                    {
                        playIds[0] = (byte)(deck.GetNextRandom() % 4);
                        playIds[1] = (byte)(deck.GetNextRandom() % 4);
                        playIds[2] = (byte)(deck.GetNextRandom() % 4);
                        if (game.Play(0, cards, playIds))
                            break;
                    }
                }
            }

            // calculate outcome
            var score = game.hand0.ScoreVersus(game.hand1);
            Interlocked.Increment(ref PlayCounts[resultsIdx]);
            Interlocked.Add(ref AccumScores[resultsIdx], score.thisScore - score.otherScore);
        }

    }

    // returns HighScorePlay
    static int FantasyKernel(
        int numCalls,
        List<int> cards
        )
    {
        var HighScorePlay = 0;
        var HighScore = 0;
        Hand HighScoreHand = default;

        var varCount = 0;
        for (int i0 = 0; i0 < 4; i0++)
        {
            for (int i1 = 0; i1 < 4; i1++)
            {
                if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) > 1)
                    continue;
                for (int i2 = 0; i2 < 4; i2++)
                {
                    if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) > 1)
                        continue;
                    for (int i3 = 0; i3 < 4; i3++)
                    {
                        if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) > 1)
                            continue;
                        if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) > 3)
                            continue;
                        if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) > 5)
                            continue;
                        if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) > 5)
                            continue;
                        for (int i4 = 0; i4 < 4; i4++)
                        {
                            if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) > 1)
                                continue;
                            if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) > 3)
                                continue;
                            if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) > 5)
                                continue;
                            if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) > 5)
                                continue;

                            for (int i5 = 0; i5 < 4; i5++)
                            {
                                if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) > 1)
                                    continue;
                                if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) > 3)
                                    continue;
                                if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) > 5)
                                    continue;
                                if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) > 5)
                                    continue;

                                for (int i6 = 0; i6 < 4; i6++)
                                {
                                    if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) > 1)
                                        continue;
                                    if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) > 3)
                                        continue;
                                    if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) > 5)
                                        continue;
                                    if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) > 5)
                                        continue;

                                    for (int i7 = 0; i7 < 4; i7++)
                                    {
                                        if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) > 1)
                                            continue;
                                        if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) > 3)
                                            continue;
                                        if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) > 5)
                                            continue;
                                        if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) > 5)
                                            continue;

                                        for (int i8 = 0; i8 < 4; i8++)
                                        {
                                            if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) > 1)
                                                continue;
                                            if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) > 3)
                                                continue;
                                            if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) > 5)
                                                continue;
                                            if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) > 5)
                                                continue;
                                            for (int i9 = 0; i9 < 4; i9++)
                                            {
                                                if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) + (i9 == 3 ? 1 : 0) > 1)
                                                    continue;
                                                if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) + (i9 == 0 ? 1 : 0) > 3)
                                                    continue;
                                                if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) + (i9 == 1 ? 1 : 0) > 5)
                                                    continue;
                                                if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) + (i9 == 2 ? 1 : 0) > 5)
                                                    continue;
                                                for (int i10 = 0; i10 < 4; i10++)
                                                {
                                                    if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) + (i9 == 3 ? 1 : 0) + (i10 == 3 ? 1 : 0) > 1)
                                                        continue;
                                                    if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) + (i9 == 0 ? 1 : 0) + (i10 == 0 ? 1 : 0) > 3)
                                                        continue;
                                                    if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) + (i9 == 1 ? 1 : 0) + (i10 == 1 ? 1 : 0) > 5)
                                                        continue;
                                                    if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) + (i9 == 2 ? 1 : 0) + (i10 == 2 ? 1 : 0) > 5)
                                                        continue;
                                                    for (int i11 = 0; i11 < 4; i11++)
                                                    {
                                                        if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) + (i9 == 3 ? 1 : 0) + (i10 == 3 ? 1 : 0) + (i11 == 3 ? 1 : 0) > 1)
                                                            continue;
                                                        if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) + (i9 == 0 ? 1 : 0) + (i10 == 0 ? 1 : 0) + (i11 == 0 ? 1 : 0) > 3)
                                                            continue;
                                                        if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) + (i9 == 1 ? 1 : 0) + (i10 == 1 ? 1 : 0) + (i11 == 1 ? 1 : 0) > 5)
                                                            continue;
                                                        if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) + (i9 == 2 ? 1 : 0) + (i10 == 2 ? 1 : 0) + (i11 == 2 ? 1 : 0) > 5)
                                                            continue;
                                                        for (int i12 = 0; i12 < 4; i12++)
                                                        {
                                                            if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) + (i9 == 3 ? 1 : 0) + (i10 == 3 ? 1 : 0) + (i11 == 3 ? 1 : 0) > 1)
                                                                continue;
                                                            if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) + (i9 == 0 ? 1 : 0) + (i10 == 0 ? 1 : 0) + (i11 == 0 ? 1 : 0) + (i12 == 0 ? 1 : 0) > 3)
                                                                continue;
                                                            if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) + (i9 == 1 ? 1 : 0) + (i10 == 1 ? 1 : 0) + (i11 == 1 ? 1 : 0) + (i12 == 1 ? 1 : 0) > 5)
                                                                continue;
                                                            if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) + (i9 == 2 ? 1 : 0) + (i10 == 2 ? 1 : 0) + (i11 == 2 ? 1 : 0) + (i12 == 2 ? 1 : 0) > 5)
                                                                continue;
                                                            for (int i13 = 0; i13 < 4; i13++)
                                                            {
                                                                if ((i0 == 3 ? 1 : 0) + (i1 == 3 ? 1 : 0) + (i2 == 3 ? 1 : 0) + (i3 == 3 ? 1 : 0) + (i4 == 3 ? 1 : 0) + (i5 == 3 ? 1 : 0) + (i6 == 3 ? 1 : 0) + (i7 == 3 ? 1 : 0) + (i8 == 3 ? 1 : 0) + (i9 == 3 ? 1 : 0) + (i10 == 3 ? 1 : 0) + (i11 == 3 ? 1 : 0) + (i12 == 3 ? 1 : 0) + (i13 == 3 ? 1 : 0) > 1)
                                                                    continue;
                                                                if ((i0 == 0 ? 1 : 0) + (i1 == 0 ? 1 : 0) + (i2 == 0 ? 1 : 0) + (i3 == 0 ? 1 : 0) + (i4 == 0 ? 1 : 0) + (i5 == 0 ? 1 : 0) + (i6 == 0 ? 1 : 0) + (i7 == 0 ? 1 : 0) + (i8 == 0 ? 1 : 0) + (i9 == 0 ? 1 : 0) + (i10 == 0 ? 1 : 0) + (i11 == 0 ? 1 : 0) + (i12 == 0 ? 1 : 0) + (i13 == 0 ? 1 : 0) > 3)
                                                                    continue;
                                                                if ((i0 == 1 ? 1 : 0) + (i1 == 1 ? 1 : 0) + (i2 == 1 ? 1 : 0) + (i3 == 1 ? 1 : 0) + (i4 == 1 ? 1 : 0) + (i5 == 1 ? 1 : 0) + (i6 == 1 ? 1 : 0) + (i7 == 1 ? 1 : 0) + (i8 == 1 ? 1 : 0) + (i9 == 1 ? 1 : 0) + (i10 == 1 ? 1 : 0) + (i11 == 1 ? 1 : 0) + (i12 == 1 ? 1 : 0) + (i13 == 1 ? 1 : 0) > 5)
                                                                    continue;
                                                                if ((i0 == 2 ? 1 : 0) + (i1 == 2 ? 1 : 0) + (i2 == 2 ? 1 : 0) + (i3 == 2 ? 1 : 0) + (i4 == 2 ? 1 : 0) + (i5 == 2 ? 1 : 0) + (i6 == 2 ? 1 : 0) + (i7 == 2 ? 1 : 0) + (i8 == 2 ? 1 : 0) + (i9 == 2 ? 1 : 0) + (i10 == 2 ? 1 : 0) + (i11 == 2 ? 1 : 0) + (i12 == 2 ? 1 : 0) + (i13 == 2 ? 1 : 0) > 5)
                                                                    continue;
                                                                varCount++;

                                                                var thisPlay =
                                                                    (i0 << 0) +
                                                                    (i1 << 2) +
                                                                    (i2 << 4) +
                                                                    (i3 << 6) +
                                                                    (i4 << 8) +
                                                                    (i5 << 10) +
                                                                    (i6 << 12) +
                                                                    (i7 << 14) +
                                                                    (i8 << 16) +
                                                                    (i9 << 18) +
                                                                    (i10 << 20) +
                                                                    (i11 << 22) +
                                                                    (i12 << 24) +
                                                                    (i13 << 26);
                                                                var hand = new Hand(thisPlay, cards);
                                                                if (!hand.IsHandComplete())
                                                                    continue;
                                                                hand.ComputeBonus();
                                                                var bonus = hand.IsFoul() ? 0 : hand.bonus[0] + hand.bonus[1] + hand.bonus[2];
                                                                if (bonus < 2)
                                                                    continue;
                                                                // is first high score?
                                                                if (HighScorePlay == 0)
                                                                {
                                                                    HighScore = bonus;
                                                                    HighScorePlay = thisPlay;
                                                                    HighScoreHand = hand;
                                                                }
                                                                else
                                                                {
                                                                    var diff = hand.ScoreVersus(HighScoreHand);
                                                                    if (diff.thisScore > diff.otherScore)
                                                                    {
                                                                        HighScore = bonus;
                                                                        HighScorePlay = thisPlay;
                                                                        HighScoreHand = hand;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return HighScorePlay;
    }


    internal struct Deck
    {
        public Random rng;
        public int[] RandomCardOrder;
        //public int[] Cards;
        public int deckLen;
        public int RandomCardIdx;
        public int[] cardsRemaining;
        public Deck(int[] cardsRemaining, Random rng)
        {
            this.cardsRemaining = cardsRemaining;
            this.rng = rng;
            deckLen = (int)cardsRemaining.Length;
            RandomCardOrder = new int[47];
            RandomCardIdx = 0;
            for (int i = 0; i < deckLen; i++)
            {
                RandomCardOrder[i] = i;
            }
        }

        public void Init()
        {
            RandomCardIdx = 0;
            // Shuffle;
            for (int i = 0; i < deckLen; i++)
            {
                int temp = RandomCardOrder[i];
                int randomIndex = GetNextRandom() % deckLen;
                RandomCardOrder[i] = RandomCardOrder[randomIndex];
                RandomCardOrder[randomIndex] = temp;
            }
        }

        //public void Shuffle()
        //{
        //    for (int i = 0; i < 52; i++)
        //    {
        //        int temp = Cards[i];
        //        int randomIndex = GetNextRandom() % 52;
        //        Cards[i] = Cards[randomIndex];
        //        Cards[randomIndex] = temp;
        //    }
        //}
        //public void RemoveCards(int[] cardsToremove)
        //{
        //    for (int i = 0; i < cardsToremove.Length; i++)
        //    {
        //        RemoveCard(cardsToremove[i]);
        //    }
        //}
        //public void RemoveCards(List<int> cardsToremove)
        //{
        //    for (int i = 0; i < cardsToremove.Count; i++)
        //    {
        //        RemoveCard(cardsToremove[i]);
        //    }
        //}
        //public void RemoveCard(int cardToremove)
        //{
        //    if (cardToremove < 0)
        //        return;
        //    for (int i = 0; i < 52; i++)
        //    {
        //        if (Cards[i] == cardToremove)
        //        {
        //            Cards[i] = -1;
        //            break;
        //        }
        //    }
        //}

        public int DrawCard()
        {
            //for (int i = 0; i < 52; i++)
            //{
            //    if (Cards[i] > -1)
            //    {
            //        var val = Cards[i];
            //        Cards[i] = -1;
            //        return val;
            //    }
            //}
            //return -1;
            return cardsRemaining[RandomCardOrder[RandomCardIdx++]];
        }

        public int[] Draw5Cards()
        {
            var cards = new int[] {
                DrawCard(),
                DrawCard(),
                DrawCard(),
                DrawCard(),
                DrawCard()
                };
            return cards;
        }
        public int[] Draw3Cards()
        {
            var cards = new int[] {
                DrawCard(),
                DrawCard(),
                DrawCard()
                };
            return cards;
        }

        public byte GetNextRandom()
        {
            var result = (byte)rng.Next();
            //if (result < 0)
            //	result *= -1;
            return result;
        }
    }

    internal struct SingleHand
    {
        public int[] Cards;
        private int _length;
        public SingleHand(bool dummy)
        {
            Cards = new int[5];
            _length = 0;
        }
        public SingleHand(List<int> cards)
        {
            Cards = new int[5];
            _length = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                AddCard(cards[i]);
            }
        }
        public SingleHand(int[] cards)
        {
            Cards = new int[5];
            _length = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                AddCard(cards[i]);
            }
        }
        public bool AddCard(int card)
        {
            if (card >= 0 && Len() < 5)
            {
                Cards[_length++] = card;
                return true;
            }
            else
                return false;
        }
        public int Len()
        {
            return _length;
        }
    }

    internal struct HandStrength
    {
        public HandRank handRank;
        public int[] ranks;
        public HandStrength(HandRank handRank, int h0 = -1, int h1 = -1, int h2 = -1, int h3 = -1, int h4 = -1)
        {
            this.handRank = handRank;
            ranks = new int[] {
                 h0,
                 h1,
                 h2,
                 h3,
                 h4 };
        }
        public bool LessThanOrEqual(HandStrength other_hand)
        {
            if (this.handRank != other_hand.handRank)
                return this.handRank < other_hand.handRank;

            if (this.ranks[0] != other_hand.ranks[0])
                return this.ranks[0] < other_hand.ranks[0];

            if (this.ranks[1] != other_hand.ranks[1])
                return this.ranks[1] < other_hand.ranks[1];

            if (this.ranks[2] != other_hand.ranks[2])
                return this.ranks[2] < other_hand.ranks[2];

            if (this.ranks[3] != other_hand.ranks[3])
                return this.ranks[4] < other_hand.ranks[4];

            return true;
        }

        public int Royalty(int handId)
        {
            if (handId == (int)HandId.Front)
            {
                return handRank switch
                {
                    HandRank.OnePair => ranks[0] >= (int)Rank.Six ? ranks[0] - (int)Rank.Five : 0,
                    HandRank.ThreeOfAKind => ranks[0] - (int)Rank.Two + 10,
                    _ => 0
                };
            }
            return handRank switch
            {
                HandRank.StraightFlush => ranks[0] == (int)Rank.Ace ? 25 : 15,
                HandRank.FourOfAKind => 10,
                HandRank.FullHouse => 6,
                HandRank.Flush => 4,
                HandRank.Straight => 2,
                HandRank.ThreeOfAKind => handId == (int)HandId.Middle ? 1 : 0, // this is doubled so it's really 2
                _ => 0
            } * (handId == (int)HandId.Middle ? 2 : 1);
        }

        private static (bool IsFlush, (int h0, int h1, int h2, int h3, int h4) ranks) is_flush(int[] _cards)
        {
            if (_cards.Length != 5)
                return (false, default);
            var suit = SolverHelper.GetSuit(_cards[0]);
            for (int i = 1; i < _cards.Length; i++)
            {
                if (SolverHelper.GetSuit(_cards[i]) != suit)
                    return (false, default);
            }
            return (true, (SolverHelper.GetRank(_cards[4]), SolverHelper.GetRank(_cards[3]), SolverHelper.GetRank(_cards[2]), SolverHelper.GetRank(_cards[1]), SolverHelper.GetRank(_cards[0])));
        }
        public static (bool IsStraight, int h0) is_straight(int[] _cards)
        {
            if (_cards.Length != 5)
                return (false, default);
            if (SolverHelper.GetRank(_cards[1]) - SolverHelper.GetRank(_cards[0]) == 1 &&
                SolverHelper.GetRank(_cards[2]) - SolverHelper.GetRank(_cards[1]) == 1 &&
                SolverHelper.GetRank(_cards[3]) - SolverHelper.GetRank(_cards[2]) == 1 &&
                SolverHelper.GetRank(_cards[4]) - SolverHelper.GetRank(_cards[3]) == 1
                )
                return (true, SolverHelper.GetRank(_cards[4]));

            // Special case for the ace to five straight
            if (
                SolverHelper.GetRank(_cards[0]) == (int)Rank.Two &&
                SolverHelper.GetRank(_cards[1]) == (int)Rank.Three &&
                SolverHelper.GetRank(_cards[2]) == (int)Rank.Four &&
                SolverHelper.GetRank(_cards[3]) == (int)Rank.Five &&
                SolverHelper.GetRank(_cards[4]) == (int)Rank.Ace
            )
                return (true, (int)Rank.Five);

            return (false, -1);
        }

        public static HandStrength ComputeStrength(SingleHand hand)
        {
            SolverHelper.SortCardsByRank(hand.Cards, hand.Len());

            if (hand.Len() == 5)
            {
                var flush = is_flush(hand.Cards);
                var straight = is_straight(hand.Cards);
                if (flush.IsFlush)
                {
                    if (straight.IsStraight)
                        return new HandStrength(HandRank.StraightFlush, straight.h0);
                    return new HandStrength(HandRank.Flush, flush.ranks.h0, flush.ranks.h1, flush.ranks.h2, flush.ranks.h3, flush.ranks.h4);
                }
                if (straight.IsStraight)
                    return new HandStrength(HandRank.Straight, straight.h0);
            }

            var rankCount = 0;
            var rankIdx = -1;
            var prevRank = -1;
            var ranks = new int[5];
            var rankCounts = new int[5];
            for (int i = 0; i < hand.Len(); i++)
            {
                var rank = SolverHelper.GetRank(hand.Cards[i]);
                if (rank != prevRank)
                {
                    rankCount++;
                    ranks[++rankIdx] = rank;
                    rankCounts[rankIdx]++;
                    prevRank = rank;
                }
                else
                    rankCounts[rankIdx]++;
            }

            var found3 = -1;
            var found2 = -1;
            var found2_2 = -1;
            for (int i = 0; i < rankCount; i++)
            {
                if (rankCounts[i] == 4)
                    return new HandStrength(HandRank.FourOfAKind, ranks[i]);
                if (rankCounts[i] == 3)
                    found3 = i;
                if (rankCounts[i] == 2)
                {
                    if (found2 == -1)
                        found2 = i;
                    else
                        found2_2 = i;
                }
            }
            if (found3 > -1 && found2 > -1)
                return new HandStrength(HandRank.FullHouse, ranks[found3], ranks[found2]);
            if (found3 > -1)
            {
                // determine 2nd rank
                int rank2;
                int rank3;
                switch (found3)
                {
                    case 0:
                        if (ranks[1] > ranks[2])
                        {
                            rank2 = ranks[1];
                            rank3 = ranks[2];
                        }
                        else
                        {
                            rank2 = ranks[2];
                            rank3 = ranks[1];
                        }
                        break;
                    case 1:
                        if (ranks[0] > ranks[2])
                        {
                            rank2 = ranks[0];
                            rank3 = ranks[2];
                        }
                        else
                        {
                            rank2 = ranks[2];
                            rank3 = ranks[0];
                        }
                        break;
                    default:
                        if (ranks[0] > ranks[1])
                        {
                            rank2 = ranks[0];
                            rank3 = ranks[1];
                        }
                        else
                        {
                            rank2 = ranks[1];
                            rank3 = ranks[0];
                        }
                        break;
                }
                return new HandStrength(HandRank.ThreeOfAKind, ranks[found3], rank2, rank3);
            }
            if (found2 > -1 && found2_2 > -1)
            {
                // determine 3rd rank
                var rank2 = -1;
                for (int i = 0; i < 5; i++)
                {
                    if (ranks[i] != found2 && ranks[i] != found2_2)
                        rank2 = ranks[i];
                }
                if (ranks[found2_2] > ranks[found2])
                    return new HandStrength(HandRank.TwoPair, ranks[found2_2], ranks[found2], rank2);
                else
                    return new HandStrength(HandRank.TwoPair, ranks[found2], ranks[found2_2], rank2);
            }
            if (found2 > -1)
            {
                var otherRanks = new int[4];
                var otherRankIdx = 0;
                for (int i = 0; i < hand.Len(); i++)
                {
                    if (ranks[i] != ranks[found2])
                    {
                        otherRanks[otherRankIdx++] = ranks[i];
                    }
                }
                if (hand.Len() == 5)
                    return new HandStrength(HandRank.OnePair, ranks[found2], otherRanks[2], otherRanks[1], otherRanks[0]);
                else
                    return new HandStrength(HandRank.OnePair, ranks[found2], otherRanks[0]);
            }
            if (hand.Len() == 5)
                return new HandStrength(HandRank.HighCard,
                    ranks[4],
                    ranks[3],
                    ranks[2],
                    ranks[1],
                    ranks[0]
                    );
            else
                return new HandStrength(HandRank.HighCard,
                ranks[2],
                ranks[1],
                ranks[0]
                );
        }
    }

    internal struct Hand
    {
        public SingleHand hand0;
        public SingleHand hand1;
        public SingleHand hand2;
        public HandStrength strength1;
        public HandStrength strength2;
        public HandStrength strength3;
        public int[] bonus;
        public int fantasyBonus;
        public Hand(SingleHand front, SingleHand middle, SingleHand back)
        {
            strength1 = new HandStrength();
            strength2 = new HandStrength();
            strength3 = new HandStrength();
            bonus = new int[3] { -1, -1, -1 };
            fantasyBonus = 0;
            hand0 = front;
            hand1 = middle;
            hand2 = back;
        }

        // fantasy hand
        public Hand(int playVal, List<int> cards)
        {
            strength1 = new HandStrength();
            strength2 = new HandStrength();
            strength3 = new HandStrength();
            bonus = new int[3] { -1, -1, -1 };
            fantasyBonus = 0;
            hand0 = new SingleHand(true);
            hand1 = new SingleHand(true);
            hand2 = new SingleHand(true);

            var discardCount = 0;
            for (int i = 0; i < 14; i++)
            {
                switch (playVal & 0x03)
                {
                    case 0:
                        if (hand0.Len() == 3)
                            return;
                        hand0.AddCard(cards[i]);
                        break;
                    case 1:
                        if (hand1.Len() == 5)
                            return;
                        hand1.AddCard(cards[i]);
                        break;
                    case 2:
                        if (hand2.Len() == 5)
                            return;
                        hand2.AddCard(cards[i]);
                        break;
                    case 3:
                        if (discardCount == 1)
                            return;
                        discardCount++;
                        break;
                }
                playVal >>= 2;
            }
        }

        // fantasy hand
        public Hand(int playVal, int[] cards)
        {
            strength1 = new HandStrength();
            strength2 = new HandStrength();
            strength3 = new HandStrength();
            bonus = new int[3] { -1, -1, -1 };
            fantasyBonus = 0;
            hand0 = new SingleHand(true);
            hand1 = new SingleHand(true);
            hand2 = new SingleHand(true);

            var discardCount = 0;
            for (int i = 0; i < 14; i++)
            {
                switch (playVal & 0x03)
                {
                    case 0:
                        if (hand0.Len() == 3)
                            return;
                        hand0.AddCard(cards[i]);
                        break;
                    case 1:
                        if (hand1.Len() == 5)
                            return;
                        hand1.AddCard(cards[i]);
                        break;
                    case 2:
                        if (hand2.Len() == 5)
                            return;
                        hand2.AddCard(cards[i]);
                        break;
                    case 3:
                        if (discardCount == 1)
                            return;
                        discardCount++;
                        break;
                }
                playVal >>= 2;
            }
        }

        public void AddCard(HandId hand_id, int card)
        {
            if ((hand_id == HandId.Front && hand0.Len() == 3) ||
                (hand_id == HandId.Middle && hand1.Len() == 5) ||
                (hand_id == HandId.Back && hand2.Len() == 5)
                )
            {
                //throw new Exception($"Hand {hand_id} is full");
                return;
            }

            switch (hand_id)
            {
                case HandId.Front:
                    hand0.AddCard(card);
                    break;
                case HandId.Middle:
                    hand1.AddCard(card);
                    break;
                case HandId.Back:
                    hand2.AddCard(card);
                    break;
                default:
                    break;
            }
        }

        public bool IsValidPlay(int[] cards, byte[] play_ids)
        {
            if (cards.Length != play_ids.Length)
                return false;

            if (IsHandComplete())
                return false;

            var frontCount = 0;
            var middleCount = 0;
            var backCount = 0;
            var discardCount = 0;
            for (int i = 0; i < play_ids.Length; i++)
            {
                if (play_ids[i] == (int)PlayId.Front)
                    frontCount++;
                else if (play_ids[i] == (int)PlayId.Middle)
                    middleCount++;
                else if (play_ids[i] == (int)PlayId.Back)
                    backCount++;
                else if (play_ids[i] == (int)PlayId.Discard)
                    discardCount++;
            }

            if (cards.Length == 5)
            {
                // Discarding in the initial 5 cards is not allowed
                if (discardCount != 0)
                    return false;
                // The front hand is limited to 3 cards
                if (frontCount > 3)
                    return false;
            }
            else if (cards.Length == 3)
            {
                // For each 3 cards draws, we have a discard exactly one
                if (discardCount != 1)
                    return false;
                var front_hand_size = hand0.Len();
                if (frontCount + front_hand_size > 3)
                    return false;

                var middle_hand_size = hand1.Len();
                if (middleCount + middle_hand_size > 5)
                    return false;

                var back_hand_size = hand2.Len();
                if (backCount + back_hand_size > 5)
                    return false;
            }
            else
                return false;

            return true;
        }

        public bool Play(int[] cards, byte[] play_ids)
        {
            if (!IsValidPlay(cards, play_ids))
                return false;
            for (int i = 0; i < cards.Length; i++)
            {
                switch (play_ids[i])
                {
                    case 0:
                        if (!hand0.AddCard(cards[i]))
                            return false;
                        break;
                    case 1:
                        if (!hand1.AddCard(cards[i]))
                            return false;
                        break;
                    case 2:
                        if (!hand2.AddCard(cards[i]))
                            return false;
                        break;
                    default:
                        break;
                }
            }
            return true;
        }

        public bool IsHandComplete()
        {
            return
                hand0.Len() == 3 &&
                hand1.Len() == 5 &&
                hand2.Len() == 5;
        }

        private void _computeStrength()
        {
            strength1 = HandStrength.ComputeStrength(hand0);
            strength2 = HandStrength.ComputeStrength(hand1);
            strength3 = HandStrength.ComputeStrength(hand2);
        }
        public bool IsFoul()
        {
            _computeStrength();

            return !(
                strength1.LessThanOrEqual(strength2) &&
                strength2.LessThanOrEqual(strength3)
                );
        }

        public void ComputeBonus()
        {
            _computeStrength();
            bonus[0] = strength1.Royalty((int)HandId.Front);
            bonus[1] = strength2.Royalty((int)HandId.Middle);
            bonus[2] = strength3.Royalty((int)HandId.Back);

            // queens or better
            // https://www.cardplayer.com/poker-news/19215-four-mistakes-you-are-making-in-pineapple-open-face-chinese
            if (bonus[0] >= 7)
                fantasyBonus = 4; // 7.5 in OFC, 4 in OFC Pinapple
            else
                fantasyBonus = 0;
        }

        public (int thisScore, int otherScore) ScoreVersus(Hand otherHand)
        {
            var self_foul = this.IsFoul();
            var other_foul = otherHand.IsFoul();
            ComputeBonus();
            otherHand.ComputeBonus();
            // If both players foul their hand, no one scores points
            if (self_foul && other_foul)
                return (0, 0);

            // Hand to hand battles
            var battle_score_self = 0;
            var battle_score_other = 0;
            if (self_foul || (!other_foul && !otherHand.strength1.LessThanOrEqual(strength1)))
                battle_score_other++;
            else if (other_foul || (!self_foul && !strength1.LessThanOrEqual(otherHand.strength1)))
                battle_score_self++;
            if (self_foul || (!other_foul && !otherHand.strength2.LessThanOrEqual(strength2)))
                battle_score_other++;
            else if (other_foul || (!self_foul && !strength2.LessThanOrEqual(otherHand.strength2)))
                battle_score_self++;
            if (self_foul || (!other_foul && !otherHand.strength3.LessThanOrEqual(strength3)))
                battle_score_other++;
            else if (other_foul || (!self_foul && !strength3.LessThanOrEqual(otherHand.strength3)))
                battle_score_self++;
            // Scooping happens when a player wins all the hand battles
            if (battle_score_self == 3)
                battle_score_self += 3;


            if (battle_score_other == 3)
                battle_score_other += 3;


            return (
                self_foul ? 0 : battle_score_self + bonus[0] + bonus[1] + bonus[2] + fantasyBonus,
                other_foul ? 0 : battle_score_other + otherHand.bonus[0] + otherHand.bonus[1] + otherHand.bonus[2] + otherHand.fantasyBonus
                );

        }
    }

    internal struct Game
    {
        public Hand hand0;
        public Hand hand1;

        public Game(Hand p0hand, Hand p1Hand)
        {
            hand0 = p0hand;
            hand1 = p1Hand;
        }

        public bool Play(int playerId, int[] cards, byte[] playIds)
        {
            var hand = playerId == 0 ? hand0 : hand1;
            if (!hand.IsValidPlay(cards, playIds))
                return false;

            for (int i = 0; i < cards.Length; i++)
            {
                if (playIds[i] != (int)PlayId.Discard)
                {
                    switch (playIds[i])
                    {
                        case 0:
                            hand.hand0.AddCard(cards[i]);
                            break;
                        case 1:
                            hand.hand1.AddCard(cards[i]);
                            break;
                        default:
                            hand.hand2.AddCard(cards[i]);
                            break;
                    };
                }
            }
            // need to copy structure back
            if (playerId == 0)
                hand0 = hand;
            else
                hand1 = hand;
            return true;
        }
    }



    public static class ThreadSafeRandomGen
    {
        private static Random _global = new Random();
        [ThreadStatic]
        private static Random _local;

        public static int Next()
        {
            Random inst = _local;
            if (inst == null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.Next();
        }
    }

    public static List<(string play, double score)> Evaluate(string p1Front, string p1Middle, string p1Back, string p1Discards, string p2Front, string p2Middle, string p2Back, string p2Discards, string p1Draw, int ProcessorCount, CancellationToken token
        )
    {
        try
        {
            //SolverHelper.SelfTest();
            // debug
            //numSimulations = 1;
            // test git

            //var simulationsPerProcessor = numSimulations / ProcessorCount;
            //numSimulations = simulationsPerProcessor * ProcessorCount;
            int numSimulations = int.MaxValue;
            var maxNumThreads = ProcessorCount;
            var numThreadSamples = numSimulations / maxNumThreads;

            var isInital = SolverHelper.CardsToInts(p1Draw).Length == 5;
            #region validate cards
            {
                int[] FixEmptyInts(int[] val)
                {
                    if (val.Length == 1 && val[0] == -1)
                        return new int[0];
                    else
                        return val;
                }
                var p1FrontInts = FixEmptyInts(SolverHelper.CardsToInts(p1Front));
                var p1MiddleInts = FixEmptyInts(SolverHelper.CardsToInts(p1Middle));
                var p1BackInts = FixEmptyInts(SolverHelper.CardsToInts(p1Back));
                var p1DiscardsInts = FixEmptyInts(SolverHelper.CardsToInts(p1Discards));
                var p2FrontInts = FixEmptyInts(SolverHelper.CardsToInts(p2Front));
                var p2MiddleInts = FixEmptyInts(SolverHelper.CardsToInts(p2Middle));
                var p2BackInts = FixEmptyInts(SolverHelper.CardsToInts(p2Back));
                var p2DiscardsInts = FixEmptyInts(SolverHelper.CardsToInts(p2Discards));
                var p1DrawInts = FixEmptyInts(SolverHelper.CardsToInts(p1Draw));


                if (p1FrontInts.Length > 3)
                    return new List<(string play, double score)>(){
                        ($"Hero Front has {p1FrontInts.Length} cards",0)
                    };
                if (p1MiddleInts.Length > 5)
                    return new List<(string play, double score)>(){
                        ($"Hero Middle has {p1MiddleInts.Length} cards",0)
                    };
                if (p1BackInts.Length > 5)
                    return new List<(string play, double score)>(){
                        ($"Hero Back has {p1BackInts.Length} cards",0)
                    };
                if (!isInital && p1DrawInts.Length != 3)
                    return new List<(string play, double score)>(){
                        ($"Hero Draw has {p1DrawInts.Length} cards",0)
                    };
                var p1SetCardCount = p1FrontInts.Length + p1MiddleInts.Length + p1BackInts.Length;
                if (p1SetCardCount != 0 && p1SetCardCount % 2 != 1)
                    return new List<(string play, double score)>(){
                        ($"Hero has invalid # of set cards: {p1SetCardCount}",0)
                    };

                if (p2FrontInts.Length > 3)
                    return new List<(string play, double score)>(){
                        ($"Villain 1 Front has {p2FrontInts.Length} cards",0)
                    };
                if (p2MiddleInts.Length > 5)
                    return new List<(string play, double score)>(){
                        ($"Villain 1 Middle has {p2MiddleInts.Length} cards",0)
                    };
                if (p2BackInts.Length > 5)
                    return new List<(string play, double score)>(){
                        ($"Villain 1 Back has {p2BackInts.Length} cards",0)
                    };
                var p2SetCardCount = p2FrontInts.Length + p2MiddleInts.Length + p2BackInts.Length;
                if (p2SetCardCount != 0 && p2SetCardCount % 2 != 1)
                    return new List<(string play, double score)>(){
                        ($"Villain 1 has invalid # of set cards: {p2SetCardCount}",0)
                    };

                if (p1SetCardCount != 0 && p2SetCardCount != 0 && Math.Abs(p1SetCardCount - p2SetCardCount) > 2)
                    return new List<(string play, double score)>(){
                        ($"Players have not played in turn",0)
                    };
            }
            #endregion


            var possiblePlays = SolverHelper.GetPossiblePlays(isInital);
            //possiblePlays = new List<byte[]>() { new byte[] { 2, 0, 3 } };
            possiblePlays = SolverHelper.GetValidPlays(isInital, possiblePlays, SolverHelper.CardsToInts(p1Front), SolverHelper.CardsToInts(p1Middle), SolverHelper.CardsToInts(p1Back), SolverHelper.CardsToInts(p1Draw));
            //possiblePlays = new List<byte[]>() { new byte[] {2,0,3 } };
            if (isInital)
            {
                possiblePlays = SolverHelper.FilterInitialPlays(
                    possiblePlays,
                    SolverHelper.CardsToInts(p1Draw),
                    SolverHelper.CardsToInts(p2Front),
                    SolverHelper.CardsToInts(p2Middle),
                    SolverHelper.CardsToInts(p2Back)
                    );
            }
            var validPlays = SolverHelper.FlattenPlays(isInital, possiblePlays);
            if (validPlays.Length == 0)
            {
                return new List<(string play, double score)>(){
                        ("???",0)
                    };
            }

            var remainingDeck = SolverHelper.RemoveCards(SolverHelper.GetFullDeck(), p1Front + p1Middle + p1Back + p1Discards + p1Draw + p2Front + p2Middle + p2Back + p2Discards);
            var remainingDeckI = SolverHelper.CardsToInts(remainingDeck);

            var p1DrawL = SolverHelper.CardsToInts(p1Draw);
            var numPlays = possiblePlays.Count;
            var p1CardList = new List<int>();
            //var p1DiscardList = new List<int[]>();
            possiblePlays.ForEach(pp =>
            {
                var thisP1Front = p1Front;
                var thisP1Middle = p1Middle;
                var thisP1Back = p1Back;
                var thisP1discards = p1Discards;
                for (int i = 0; i < pp.Length; i++)
                {
                    var thisDrawCard = SolverHelper.IntToCard(p1DrawL[i]);

                    switch (pp[i])
                    {
                        case 0:
                            thisP1Front += thisDrawCard;
                            break;
                        case 1:
                            thisP1Middle += thisDrawCard;
                            break;
                        case 2:
                            thisP1Back += thisDrawCard;
                            break;
                        default:
                            thisP1discards += thisDrawCard;
                            break;
                    }
                }
                p1CardList.AddRange(SolverHelper.CardsToInts(thisP1Front, 3));
                p1CardList.AddRange(SolverHelper.CardsToInts(thisP1Middle, 5));
                p1CardList.AddRange(SolverHelper.CardsToInts(thisP1Back, 5));
            });

            var p2CardList = new List<int>();
            p2CardList.AddRange(SolverHelper.CardsToInts(p2Front, 3));
            p2CardList.AddRange(SolverHelper.CardsToInts(p1Middle, 5));
            p2CardList.AddRange(SolverHelper.CardsToInts(p1Back, 5));

            var accumScores = new int[numPlays];
            var playCounts = new int[numPlays];

            var startTime = DateTime.Now;

            Parallel.For(0, maxNumThreads, idx =>
            {
                MonteCarloKernel(idx,
                    numPlays,
                    maxNumThreads,
                    numThreadSamples,
                    p1CardList.ToArray(),
                    p2CardList.ToArray(),
                    remainingDeckI,
                    new Random(),
                    accumScores,
                    playCounts,
                    token
                    );
            });
            var EndTime = DateTime.Now;

            var results = Enumerable.Range(0, accumScores.Length).ToList().Select(i =>
            {
                var score = accumScores[i] / (double)(playCounts[i] > 0 ? playCounts[i] : 1);
                var play =
                SolverHelper.playToString(validPlays[i * (isInital ? 5 : 3) + 0]) +
                SolverHelper.playToString(validPlays[i * (isInital ? 5 : 3) + 1]) +
                SolverHelper.playToString(validPlays[i * (isInital ? 5 : 3) + 2]);
                if (isInital)
                {
                    play +=
                    SolverHelper.playToString(validPlays[i * (isInital ? 5 : 3) + 3]) +
                    SolverHelper.playToString(validPlays[i * (isInital ? 5 : 3) + 4]);
                }
                return (play, score);
            })
                .OrderByDescending(s => s.score)
                //.Where(s => s.score > 0)
                //.Take(20)
                .ToList();
            var topScore = results.First().score;
            var numSimulated = playCounts.Sum();
            results.ForEach(r =>
            {
                Trace.WriteLine($"Play: {r.play} Score: {Math.Round(r.score, 2)}  Delta: {Math.Round(r.score - topScore, 2)}");
            });
            var duration = EndTime - startTime;
            Trace.WriteLine($"Completed {numSimulated} monte carlo simulations in {duration:mm}m {duration:ss}s {duration:ff}ms");
            Trace.WriteLine($"===========================================");

            return results;
        }
        catch (Exception ex)
        {
            throw;
        }
    }



    public static (string play, int royalties) EvaluateFantasy(string cards)
    {
        try
        {
            var cardInts = SolverHelper.CardsToInts(cards);
            if (cardInts.Length != 14)
                throw new Exception($"Card count must be 14.  {cardInts.Length} found");
            var numCalls = (1 << 28) - 1;
            var startTime = DateTime.Now;
            var topPlay = FantasyKernel(numCalls, cardInts.ToList());
            var EndTime = DateTime.Now;
            var bestHand = new Hand(topPlay, cardInts.ToList());

            var displayResult = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                displayResult += SolverHelper.IntToCard(bestHand.hand0.Cards[i]) + " ";
            }
            displayResult += Environment.NewLine;
            for (int i = 0; i < 5; i++)
            {
                displayResult += SolverHelper.IntToCard(bestHand.hand1.Cards[i]) + " ";
            }
            displayResult += Environment.NewLine;
            for (int i = 0; i < 5; i++)
            {
                displayResult += SolverHelper.IntToCard(bestHand.hand2.Cards[i]) + " ";
            }
            Trace.WriteLine(displayResult);
            var duration = EndTime - startTime;
            Trace.WriteLine($"Completed in {duration:ss}s {duration:ff}ms");
            Trace.WriteLine($"===========================================");
            bestHand.ComputeBonus();
            var royalties = bestHand.IsHandComplete() && bestHand.IsFoul() ? bestHand.bonus[0] + bestHand.bonus[1] + bestHand.bonus[2] : 0;
            return (displayResult, royalties);
        }
        catch (Exception ex)
        {
        }
        return default;

    }

    #region enums
    public enum Suit : byte
    {
        Spade = 0,
        Heart = 1,
        Diamond = 2,
        Club = 3
    }
    public enum HandId : byte
    {
        Front = 0,
        Middle = 1,
        Back = 2
    }
    public enum PlayerId : byte
    {
        Hero, Villain
    }
    public enum PlayId : byte
    {
        Front, Middle, Back, Discard
    }
    public enum Rank : byte
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14,
    }
    public enum HandRank : byte
    {
        HighCard = 0,
        OnePair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8,
        //Unknown = 99,
    }
    #endregion

    public class SolverHelper
    {
        public static List<byte[]> _allInitialPlays;
        //public static byte[] AllNonInitialPlays;
        public static List<byte[]> _allNonInitialPlays;
        public static List<byte[]> GetPossiblePlays(bool isInitial)
        {
            if (isInitial)
            {
                if (_allInitialPlays == default)
                {
                    _allInitialPlays = new List<byte[]>();
                    for (byte i0 = 0; i0 < 3; i0++)
                        for (byte i1 = 0; i1 < 3; i1++)
                            for (byte i2 = 0; i2 < 3; i2++)
                                for (byte i3 = 0; i3 < 3; i3++)
                                    for (byte i4 = 0; i4 < 3; i4++)
                                    {
                                        var play_ids = new byte[] {
                                        i0,
                                        i1,
                                        i2,
                                        i3,
                                        i4
                                    };
                                        _allInitialPlays.Add(play_ids);
                                    }
                    _allInitialPlays = _allInitialPlays.Where(p => p.Count(p2 => p2 == 0) <= 3).ToList();
                }
                return _allInitialPlays;
            }
            else
            {
                if (_allNonInitialPlays == default)
                {
                    _allNonInitialPlays = new List<byte[]>();
                    for (byte i0 = 0; i0 < 4; i0++)
                        for (byte i1 = 0; i1 < 4; i1++)
                            for (byte i2 = 0; i2 < 4; i2++)
                            {
                                var discardCount =
                                    (i0 == 3 ? 1 : 0) +
                                    (i1 == 3 ? 1 : 0) +
                                    (i2 == 3 ? 1 : 0);
                                if (discardCount == 1)
                                {
                                    _allNonInitialPlays.Add(new byte[] { i0, i1, i2 });
                                }
                            }
                }
                return _allNonInitialPlays;
            }
        }

        public static List<byte[]> GetValidPlays(bool isInitial, List<byte[]> plays, int[] FrontCards, int[] MiddleCards, int[] BackCards, int[] DrawCards)
        {
            // eliminate any hands that are invalid or would foul
            return plays.Where(p =>
            {
                var hand = new OFCevaluator.Hand(
                    new OFCevaluator.SingleHand(FrontCards),
                    new OFCevaluator.SingleHand(MiddleCards),
                    new OFCevaluator.SingleHand(BackCards)
                    );
                if (!hand.IsValidPlay(DrawCards, p))
                    return false;
                for (int i = 0; i < p.Length; i++)
                {
                    switch (p[i])
                    {
                        case 0:
                            if (!hand.hand0.AddCard(DrawCards[i]))
                                return false;
                            break;
                        case 1:
                            if (!hand.hand1.AddCard(DrawCards[i]))
                                return false;
                            break;
                        case 2:
                            if (!hand.hand2.AddCard(DrawCards[i]))
                                return false;
                            break;
                        default:
                            break;
                    }
                }
                if (hand.IsHandComplete() && hand.IsFoul())
                    return false;

                return true;
            }).ToList();
        }

        public static List<byte[]> FilterInitialPlays(List<byte[]> plays, int[] DrawCards, int[] VillainFrontCards, int[] VillainMiddleCards, int[] VillainBackCards)
        {
            var result = FilterInitialPlays(plays, DrawCards, VillainFrontCards, VillainMiddleCards, VillainBackCards, false);
            if (result.Count == 0)
                result = FilterInitialPlays(plays, DrawCards, VillainFrontCards, VillainMiddleCards, VillainBackCards, true);
            return result;
        }
        private static List<byte[]> FilterInitialPlays(List<byte[]> plays, int[] DrawCards, int[] VillainFrontCards, int[] VillainMiddleCards, int[] VillainBackCards, bool defaultResult)
        {
            /*
    From: https://alastairkerr.co.uk/Dissertation%204177303.pdf

    4.2.2 First 5 Card Placements
    For the Intelligent Agent to determine the optimal placements for the first 5 cards, analysis is first performed
    to find all permutations of possible card placements. This results in an initial total of
    13
    5
    
    = 1287 states, but
    2
    this can be pruned down further by removing duplicate states with the same cards in each row but ordered
    differently, reducing the maximum possible states to consider to around 240 which is a more manageable
    amount although still isn't ideal.
    Here, heuristics and domain specifc knowledge are implemented to further prune the number of states
    considered. The hand is evaluated, mapping the frequencies of each occurring rank in a histogram. In the
    case that the hand contains a Pair, Three of a Kind, or Four of a Kind then any states which do not place
    these cards in the same row can be pruned as they would almost inevitably be sub-optimal. The better the
    initial starting hand, the fewer possible states the Agent must consider.
    In the worst case scenario with 5 unique ranks and no flush or straight potential, some last resort pruning
    is implemented, removing certain state combinations that can be considered unlikely to produce optimal
    results, such as placing all cards in middle, or having three cards dumped in the top row. This reduces the
    number of states to consider, although will still likely mean the Agent must consider ∼ 200 different states.
    Considering the requirement for the Agent to return its move in 5 seconds, this will likely result in suboptimal
    placements as there will only be ∼ 5
    200 seconds worth of computation time spent simulating each state. Such
    a limitation is unfortunate, and is discussed later in this report in the Evaluation section.
    The results of each game playout are stored and after all simulations have been completed the results are
    aggregated and the most promising scoring state's card placements are chosen.

            */
            if (DrawCards.Count() != 5)
                throw new Exception($"Need 5 cards for inital play.  Found {DrawCards.Count()}");

            #region count ranks and suits
            var ranksByCount = DrawCards
                .GroupBy(c => GetRank(c))
                .Select(n => (

                    rank: n.Key,
                    rankCount: n.Count()
                )
                )
                .OrderByDescending(n => n.rankCount)
                .ThenBy(n => n.rank)
                .ToArray();
            var ranksByRank = ranksByCount.OrderBy(n => n.rank).ToArray();
            var suits = DrawCards
                    .GroupBy(c => GetSuit(c))
                    .Select(n => (

                        suit: n.Key,
                        suitCount: n.Count()
                    )
                    )
                    .OrderByDescending(n => n.suitCount).ThenByDescending(n => n.suit)
                    .ToArray();
            #endregion

            var cards = DrawCards.OrderBy(c => GetRank(c)).ToList();

            bool checkStraight()
            {
                // If there is less than 5 cards or any pair, three of kind
                // or four of a kind, there cannot be a straight
                if (ranksByCount.Length != 5)
                    return false;

                // If there is no pair and the smallest one is 4 values
                // away from the biggest, we have a straight
                if (ranksByCount[^1].rank == ranksByCount[0].rank - 4)
                    return true;

                // Special case for the ace to five straight
                if (ranksByRank[0].rank == (int)Rank.Two &&
                    ranksByRank[1].rank == (int)Rank.Three &&
                    ranksByRank[2].rank == (int)Rank.Four &&
                    ranksByRank[3].rank == (int)Rank.Five &&
                    ranksByRank[4].rank == (int)Rank.Ace)
                    return true;

                return false;
            }
            (bool isStraight, List<int> highRank) checkStraight4()
            {
                // If there is less than 4 cards or any pair, three of kind
                // or four of a kind, there cannot be a straight
                if (ranksByCount.Length < 4)
                    return (false, default);

                // remove outlier
                var ranksToCheck = ranksByRank.AsSpan();
                if (ranksToCheck.Length == 5)
                {
                    if (ranksToCheck[0].rank == ranksToCheck[1].rank - 1)
                        ranksToCheck = ranksToCheck[..^1];
                    else
                        ranksToCheck = ranksToCheck[1..];
                }

                if (ranksToCheck[0].rank == ranksToCheck[3].rank - 3)
                    return (true, new List<int>() { ranksToCheck[0].rank, ranksToCheck[1].rank, ranksToCheck[2].rank, ranksToCheck[3].rank });

                if (ranksToCheck[0].rank == (int)Rank.Two && ranksToCheck[3].rank == (int)Rank.Ace) // Special case for 1 to 4
                    return (true, new List<int>() { (int)Rank.Two, (int)Rank.Three, (int)Rank.Four, (int)Rank.Ace });

                return (false, default);
            }
            int getRankPlayedOnLevelCount(byte[] p, int rank, int level)
            {
                var levelRankCount = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (p[i] == level && GetRank(DrawCards[i]) == rank)
                        levelRankCount++;
                }
                return levelRankCount;
            }
            string playsToString(byte[] ps)
            {
                var r = string.Empty;
                for (int i = 0; i < ps.Length; i++)
                {
                    r += playToString(ps[i]);
                }
                return r;
            }
            bool isQuads = ranksByCount.Length == 2 && ranksByCount[0].rankCount == 4;
            var isStraight = checkStraight();
            var isStraight4 = checkStraight4();
            var flushCount = suits.Max(s => s.suitCount);
            var flushSuit0 = suits[0];
            var flushSuit1 = suits.Length >= 1 ? suits[1] : suits[0];
            var isFullHouse = ranksByCount.Length == 2;
            var isTrips = !isFullHouse && ranksByCount[0].rankCount == 3;
            var isTwoPair = ranksByCount.Length == 3;
            var isOnePair = ranksByCount.Length == 4;

            //plays = new List<byte[]>() { new byte[]{ 1, 1, 2, 2, 2 } };

            return plays.Where(p =>
            {
                // debugging
                //// BBFMF
                //if (p[0] == 2 && p[1] == 2 && p[2] == 0 && p[3] == 1 && p[4] == 0)
                //{

                //}

                var levelPlays = new int[] {
                p.Count(p2 => p2 == 0),
                p.Count(p2 => p2 == 1),
                p.Count(p2 => p2 == 2)
            }.ToArray();

                // don't allow playing 3 cards in the front
                if (levelPlays[0] == 3)
                    return false;
                // don't allow playing 2 cards in the front unless > Q
                if (levelPlays[0] == 2 && ranksByCount[0].rank < (int)Rank.Queen)
                    return false;

                // if straightflush, always play 5 cards on the back
                if (isStraight && flushCount == 5)
                {
                    var isCand = p.Count(v => v == 2) >= 5;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + " : Straight Flush");
                    return isCand;
                }
                // if quads, always play on the back
                if (isQuads)
                {
                    var isCand = getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 2) == 4;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + " : Quads");
                    return isCand;
                }

                // if straight, play at least 4 cards on the back
                if (isStraight)
                {
                    var isCand = p.Count(v => v == 2) >= 4;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + " : Straight");
                    return isCand;
                }
                // if flush, play at least 4 cards on the back
                if (flushCount == 5)
                {
                    var isCand = p.Count(v => v == 2) >= 4;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + ": Flush");
                    return isCand;
                }
                // if flush draw, ok play at least 2 cards on the back
                if (flushCount >= 2)
                {
                    var suitsOnBack = new List<int>();
                    for (int i = 0; i < p.Length; i++)
                    {
                        if (p[i] == 2)
                            suitsOnBack.Add(GetSuit(cards[i]));
                    }

                    var bottomSuits = suitsOnBack
                    .GroupBy(c => c)
                    .Select(n => (

                        suit: n.Key,
                        suitCount: n.Count()
                    )
                    )
                    .OrderByDescending(n => n.suitCount).ThenByDescending(n => n.suit)
                    .FirstOrDefault();
                    var isCand = bottomSuits.suitCount >= 2;
                    if (isCand)
                    {
                        Trace.WriteLine(playsToString(p) + ": Flush draw on bottom");
                        return isCand;
                    }
                }
                // if full house, play at least the 3 cards on the back
                if (isFullHouse)
                {
                    var isCand = getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 2) == 3;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + ": Full House");
                    return isCand;
                }
                // trips always played on the back
                if (isTrips)
                {
                    var isCand = getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 2) == 3;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + ": Trips");
                    return isCand;
                }

                // if two pair, play at least one of the pairs somewhere
                if (isTwoPair)
                {
                    var isCand =
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 0) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 1) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 2) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[1].rank, 0) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[1].rank, 1) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[1].rank, 2) == 2;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + ": two pair");
                    return isCand;
                }
                // if one pair, play it somewhere
                // (optional)
                if (isOnePair)
                {
                    var isCand =
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 0) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 1) == 2 ||
                    getRankPlayedOnLevelCount(p, ranksByCount[0].rank, 2) == 2;
                    if (isCand)
                        Trace.WriteLine(playsToString(p) + ": Straight Flush");
                    return isCand;
                }

                // if straight draw, ok to go for it
                if (!isStraight && isStraight4.isStraight)
                {
                    // don't play all 5 cards on the back
                    if (levelPlays[2] == 5)
                        return false;
                    else if (levelPlays[2] < 3) // straight draw on back
                        return false;
                    else
                    { // ensure those at least 3 cards are part of the straight
                        var ranksOnBack = new List<int>();
                        for (int i = 0; i < p.Length; i++)
                        {
                            ranksOnBack.Add(GetRank(cards[i]));
                        }
                        var commonRanks = isStraight4.highRank.Intersect(ranksOnBack).ToList();
                        if (commonRanks.Count >= 3)
                        {
                            Trace.WriteLine(playsToString(p) + ": 4 straight cards");
                            return true;
                        }
                    }
                }

                // don't allow playing 5 cards in the middle
                if (levelPlays[1] == 5)
                    return false;

                // ace in the middle with K or Q on top
                {
                    var anyAceInMiddle = false;
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (p[i] != 1)
                            continue;

                        if (p[i] == 1 && GetRank(DrawCards[i]) == (int)Rank.Ace)
                        {
                            anyAceInMiddle = true;
                            break;
                        }
                    }
                    var anyKingQueenOnTop = false;
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (p[i] != 0)
                            continue;
                        if (GetRank(DrawCards[i]) == (int)Rank.King || GetRank(DrawCards[i]) == (int)Rank.Queen)
                        {
                            anyKingQueenOnTop = true;
                            break;
                        }
                    }

                    var isCand = anyAceInMiddle && anyKingQueenOnTop;
                    if (isCand)
                    {
                        Trace.WriteLine(playsToString(p) + ": Ace in middle with K/Q up front");
                        return true;
                    }
                }

                return defaultResult;
            }).ToList();
        }

        public static byte[] FlattenPlays(bool isInitial, List<byte[]> plays)
        {
            if (plays == default || plays.Count == 0)
                return new byte[0];
            var plays2 = new byte[plays.Count * (isInitial ? 5 : 3)];
            var idx = 0;
            plays.ForEach(p =>
            {
                p.ToList().ForEach(v => { plays2[idx++] = v; });
            });
            return plays2;
        }

        public static List<byte[]> SortPlays(bool isInitial, List<byte[]> plays, int[] FrontCards, int[] MiddleCards, int[] BackCards, int[] DrawCards)
        {
            // ensure the plays are valid and complete
            plays = plays.Where(p =>
            {
                var hand0 = new OFCevaluator.Hand(
                    new OFCevaluator.SingleHand(FrontCards),
                    new OFCevaluator.SingleHand(MiddleCards),
                    new OFCevaluator.SingleHand(BackCards)
                    );
                if (!hand0.IsValidPlay(DrawCards, p))
                    return false;
                return hand0.IsHandComplete();
            }).ToList();

            plays.Sort((v0, v1) =>
            {
                var hand0 = new OFCevaluator.Hand(
                    new OFCevaluator.SingleHand(FrontCards),
                    new OFCevaluator.SingleHand(MiddleCards),
                    new OFCevaluator.SingleHand(BackCards)
                    );
                hand0.Play(DrawCards, v0);

                var hand1 = new OFCevaluator.Hand(
                    new OFCevaluator.SingleHand(FrontCards),
                    new OFCevaluator.SingleHand(MiddleCards),
                    new OFCevaluator.SingleHand(BackCards)
                    );
                hand0.Play(DrawCards, v1);
                var compare = hand0.ScoreVersus(hand1);
                return compare switch
                {
                    var x when x.thisScore > compare.otherScore => 1,
                    var x when x.thisScore < compare.otherScore => -1,
                    _ => 0
                };

            });

            return plays;
        }

        public static int CardToInt(string card)
        {
            if (string.IsNullOrWhiteSpace(card))
                throw new Exception($"Invalid card: {card}");
            card = card.Replace(" ", "").ToUpper();
            if (card.Length != 2)
                throw new Exception($"Invalid card: {card}");
            byte suit = card[1] switch
            {
                'C' => (byte)Suit.Club,
                'D' => (byte)Suit.Diamond,
                'H' => (byte)Suit.Heart,
                'S' => (byte)Suit.Spade,
                _ => throw new Exception($"Invalid Suit: {card}")
            };
            var rank = card[0].ToString();
            var int_rank = "23456789TJQKA".IndexOf(rank);
            if (int_rank < 0)
                throw new Exception($"Invalid Rank: {rank}");
            return CardPack(int_rank + (byte)Rank.Two, suit);
        }
        public static int CardPack(int rank, int suit)
        {
            return (rank << 2) | suit;
        }
        public static int[] CardsToInts(string cards, int? length = null)
        {
            if (string.IsNullOrWhiteSpace(cards))
                return Enumerable.Range(0, length ?? 1).Select(i => -1).ToArray();
            cards = cards.Replace(" ", "").ToUpper();
            if (cards.Length % 2 != 0)
                throw new Exception($"Invalid cards: {cards}");
            var cardsLength = cards.Length / 2;
            var results = new int[length ?? cardsLength];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = -1;
            }
            for (int i = 0; i < cardsLength; i++)
            {
                results[i] = CardToInt(cards.Substring(i * 2, 2));
            }
            return results;
        }

        public static string IntToCard(int cardInt)
        {
            var rank = "0123456789TJQKA".Substring(GetRank(cardInt), 1);
            var suit = "SHDC".Substring(GetSuit(cardInt), 1);
            return rank + suit;
        }
        public static string IntsToCard(IEnumerable<int> cardInts)
        {
            var cards = cardInts.Select(ci => IntToCard(ci));
            return string.Join(" ", cards);
        }

        public static string playToString(int play)
        {
            return play switch
            {
                0 => "F",
                1 => "M",
                2 => "B",
                3 => "D",
                _ => "?"
            };
        }

        public static int GetRank(int card)
        {
            return card >> 2;
        }
        public static int GetSuit(int card)
        {
            return card & 0x03;
        }

        public static bool SelfTest()
        {
            var Cards = new int[52];
            {
                int i = 0;
                for (int suit = 0; suit < 4; suit++)
                {
                    for (int rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
                    {
                        Cards[i++] = SolverHelper.CardPack(rank, suit);
                    }
                }
            }

            for (int i = 0; i < Cards.Length; i++)
            {
                var iVal = Cards[i];
                var sVal = IntToCard(iVal);
                var iVal2 = CardToInt(sVal);
                if (iVal != iVal2)
                    throw new Exception("Card Conversion failed!");
            }

            Shuffle(Cards);
            Trace.WriteLine(IntsToCard(Cards));

            SortCardsByRank(Cards, 52);

            Trace.WriteLine(IntsToCard(Cards));
            return true;
        }
        public static string GetFullDeck()
        {
            var Cards = new int[52];
            {
                int i = 0;
                for (int suit = 0; suit < 4; suit++)
                {
                    for (int rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
                    {
                        Cards[i++] = SolverHelper.CardPack(rank, suit);
                    }
                }
            }
            return IntsToCard(Cards);
        }
        public static int[] GetFullDeckInts()
        {
            var sVal = GetFullDeck();
            return CardsToInts(sVal);
        }

        public static int[] RemoveCards(int[] deck, int[] cardsToRemove)
        {
            var deckL = deck.ToList();
            var cardsToRemoveL = cardsToRemove.ToList();
            var numRemoved = deckL.RemoveAll(v => cardsToRemoveL.IndexOf(v) > -1);
            return deckL.ToArray();
        }
        public static string RemoveCards(string deck, string cardsToRemove)
        {
            var result = RemoveCards(CardsToInts(deck), CardsToInts(cardsToRemove));
            return IntsToCard(result);
        }


        public static void Shuffle(int[] Cards)
        {
            var random = new System.Random();
            for (int i = 0; i < Cards.Length; i++)
            {
                int temp = Cards[i];
                int randomIndex = random.Next() % 52;
                Cards[i] = Cards[randomIndex];
                Cards[randomIndex] = temp;
            }
        }

        //public static int[] ListToInt(List<int> cards)
        //{
        //	Cards = new int[4];
        //	_length = 0;
        //	for (int i = 0; i < cards.Length; i++)
        //	{
        //		AddCard(cards[i]);
        //	}
        //}

        public static void SortCardsByRank(int[] array, int array_size)
        {
            int tmp, min_key;

            for (int j = 0; j < array_size - 1; j++)
            {
                min_key = j;

                for (int k = j + 1; k < array_size; k++)
                {
                    if (GetRank(array[k]) < GetRank(array[min_key]))
                    {
                        min_key = k;
                    }
                }

                tmp = array[min_key];
                array[min_key] = array[j];
                array[j] = tmp;
            }
        }
    }
}


