using ANetConnect;
using CriticalValueService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SymmetricSecurityKey = Microsoft.IdentityModel.Tokens.SymmetricSecurityKey;

namespace CriticalValueService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        /// <summary>
        /// 获取Token
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("token")]
        public ActionResult GetAccessToken(string username, string password)
        {
            if (password == null)
            {
                password = "";
            }
            if (!Fun.VerifyUserPwd(password, Fun.StrToInt(username)))
            {
                return BadRequest($"密码验证失败！用户名：{username},密码：{password}");
            }
            //if (username != "admin" || password != "admin")
            //    return BadRequest("Invalid Request");

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, ""),
             };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenParameter.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(TokenParameter.Issuer, TokenParameter.Audience, claims, expires: DateTime.UtcNow.AddMinutes(TokenParameter.AccessExpiration), signingCredentials: credentials);
            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return Ok(token);
        }
    }

}
