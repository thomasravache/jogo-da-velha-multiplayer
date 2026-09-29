using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0030: Lobby Cyber Arena

public class LobbyCyberArenaTests
{
    private static readonly string[] ExpectedParameters = new[]
        {
            "CreatedRoomCode:String", "DifficultyChanged:EventCallback`1", "InputRoomCode:String",
            "InputRoomCodeChanged:EventCallback`1", "IsWaiting:Boolean", "OnCreateRoom:EventCallback",
            "OnJoinRoom:EventCallback", "OnPlayOnline:EventCallback", "OnPlaySolo:EventCallback",
            "PlayerName:String", "PlayerNameChanged:EventCallback`1", "RoomErrorMessage:String",
            "SelectedDifficulty:AiDifficulty",
        }.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    [Fact(DisplayName = "SPEC-0030:CH-01 — API pública do Lobby (parâmetros) permanece a mesma")]
    [Trait("Category", "SPEC-0030:CH-01")]
    public void Lobby_PublicParameters_ShouldRemainUnchanged()
    {
        var actual = typeof(Lobby)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(p => $"{p.Name}:{p.PropertyType.Name}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();


        Assert.Equal(ExpectedParameters, actual);
    }
}
