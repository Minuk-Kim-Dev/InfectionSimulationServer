﻿using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Server.DB;
using Server.Game;
using ServerCore;
using System;
using System.Linq;
using System.Numerics;
using static Server.DB.DataModel;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        // 점수 등록
        public void RegisterScore(string accountId, string position, int finalScore, DateTime date)
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    // AccountId에 해당하는 계정을 찾기
                    var account = db.Accounts
                        .FirstOrDefault(a => a.Id == accountId);

                    if (account == null)
                    {
                        Console.WriteLine("계정을 찾을 수 없습니다.");
                        return;
                    }

                    // ScoreDb 객체 생성 및 값 할당
                    ScoreDb newScore = new ScoreDb()
                    {
                        AccountId = accountId,
                        Account = account,    // 외래 키 관계 설정
                        Position = position,  // 수행한 직무
                        FinalScore = finalScore,  // 최종 점수
                        GameDate = date   // 게임 완료 날짜 및 시간
                    };

                    // ScoreDb에 새 점수 추가
                    db.Scores.Add(newScore);
                    db.SaveChanges();  // 변경 사항 저장
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendScores(string accountId, string position)
        {
            using (AppDbContext db = new AppDbContext())
            {
                // 해당 플레이어의 특정 Position에 대한 모든 점수를 조회
                var scores = db.Scores
                    .Include(s => s.Account)
                    .Where(s => s.Position == position)
                    .OrderByDescending(s => s.GameDate)  // 날짜 순서대로 정렬
                    .ToList();

                // 최근 등록된 점수 (최신 점수)
                var recentScore = scores.FirstOrDefault();

                if (recentScore == null)
                {
                    // 만약 점수가 없다면 그냥 반환
                    return;
                }

                // 클라이언트에 보낼 패킷 생성
                S_PlayerRank rankPacket = new S_PlayerRank();

                foreach (var score in scores)
                {
                    ScoreInfo scoreInfo = new ScoreInfo()
                    {
                        ScoreId = score.ScoreId,
                        Position = score.Position,
                        FinalScore = score.FinalScore,
                        GameDate = score.GameDate.Ticks,
                        AccountId = score.AccountId,
                        PlayerName = score.Account.Name,
                    };

                    rankPacket.Scores.Add(scoreInfo);
                }

                // 패킷의 GameId는 최근 점수의 ScoreId로 설정
                rankPacket.GameId = recentScore.ScoreId;

                // 클라이언트로 패킷 전송
                Send(rankPacket);
            }
        }
    }
}
