using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Roulette.Models;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Roulette.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RouletteController : ControllerBase
    {
        const string cacheKey = "list";

        private readonly ILogger<RouletteController> _logger;
        private readonly IMemoryCache _redis;
        private readonly Random _random = new Random();

        public RouletteController(ILogger<RouletteController> logger, IMemoryCache redis)
        {
            _logger = logger;
            _redis = redis;
        }

        [HttpGet]
        [Route("list")]
        public object List()
        {
            List<RouletteData> list = null;
            try
            {
                list = GetRoulettes();
            }
            catch (Exception ex)
            {
                _logger.LogError("Can't load roulettes" + ex.Message);
            }
            return list;
        }

        [HttpGet]
        [Route("create")]
        public int Create()
        {
            try
            {
                int id = CreateRoulette();
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError("Can't create roulette " + ex.Message);
            }
            return 0;
        }

        [HttpGet]
        [Route("open")]
        public string Open(int id)
        {
            var result = "Error:";
            try
            {
                RouletteData item = GetRoulette(id);
                if (item != null)
                {
                    item.open = true;
                    SaveRoulette(item);
                    result = "OK";
                }
                else
                {
                    result += "Roullete doesn't exist";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Can't open roulette " + ex.Message);
                result += "Can't open roulette " + ex.Message;
            }
            return result;
        }

        [HttpPost]
        [Route("closebet")]
        public async Task<string> CloseBet(int id)
        {
            return await CheckWinners(id);
        }


        [HttpPost]
        [Route("bet")]
        public async Task<string> Bet(int id, int? number, string color, int money)
        {
            try
            {
                string user = HttpContext.Request.Headers["userid"];
                if (!string.IsNullOrEmpty(user) && id > 0)
                {
                    return await MakeBet(id, number, color, money, user);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return "Error";
        }

        private Task<string> CheckWinners(int id)
        {
            var result = "Error:";
            var itm = GetRoulette(id);
            if (itm != null && itm.open)
            {
                // determinar el número ganador
                int winner = _random.Next(0, 36);
                string color = (winner % 2) == 0 ? "Red" : "Black";
                foreach (var bet in itm.bets)
                {
                    if (bet.Number == winner)
                    {
                        bet.Winner = true;
                        bet.Prize = bet.Bet * 5;
                    }
                    else if (bet.Color == color)
                    {
                        bet.Winner = true;
                        bet.Prize = bet.Bet * 1.8;
                    }
                }
                itm.open = false;
                SaveRoulette(itm);
                result = "Ok";
            }
            else
            {
                if(itm == null)
                {
                    result += "Roulette doesn't exist";
                }
                else
                {
                    result += "Roulette is closed";
                }
                
            }
            return Task<string>.FromResult(result);
        }

        private Task<string> MakeBet(int id, int? number, string color, int betMoney, string username)
        {
            var result = "Error: ";
            var roulette = GetRoulette(id);
            if(roulette == null || !roulette.open)
            {
                result += "Roulette is closed or it doesn't exist";
            }
            else if ((number == null && color != null) || (number != null && color == null))
            {
                if(((number != null && number >= 0 && number <= 36) || color != null) && (betMoney >0 && betMoney <= 10000))
                        {
                    var itm = new BetData()
                    {
                        Number = number ?? -1,
                        Color = color,
                        Bet = betMoney,
                        Username = username
                    };
                    roulette.bets.Add(itm);
                    SaveRoulette(roulette);
                    result = "Ok";
                }
                else
                {
                    result += "Check values [number from 0 to 36 or color: Red or Black]";
                }
            }
            else
            {
                result += "Cannot make bet to number and color, choose one";
            }
            return Task<string>.FromResult(result);
        }

        private int CreateRoulette()
        {
            var list = GetRoulettes();
            int id = list.Count + 1;
            list.Add(new RouletteData() { id = id, open = false, bets = new List<BetData>() });
            SetRoulettes(list);
            return id;
        }

        private void SaveRoulette(RouletteData item)
        {
            var list = GetRoulettes();
            var exist = list.Where(x => x.id == item.id).FirstOrDefault();
            if(exist != null)
            {
                exist = item;
            }
            SetRoulettes(list);
        }
        private void SetRoulettes(List<RouletteData> list)
        {
            _redis.Set(cacheKey, list);
        }

        private RouletteData GetRoulette(int id)
        {
            var item = new RouletteData();
            var list = GetRoulettes();
            var existitem = list.Where(x => x.id == id).FirstOrDefault();
            if (existitem != null)
            {
                item = existitem;
            }
            return item;
        }

        private List<RouletteData> GetRoulettes()
        {
            List<RouletteData> list;
            var exist = _redis.TryGetValue(cacheKey, out list);
            if(list == null)
            {
                list = new List<RouletteData>();
            }
            return list;
        }
    }
}
