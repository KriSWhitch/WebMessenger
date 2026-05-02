using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Services;
using WebMessenger.Api.Tests.Shared;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;

namespace WebMessenger.Api.Tests.Unit.Services;

/// <summary>
/// AutoFixture + AutoMoq demonstration using ChatService.
///
/// This file focuses on advanced techniques:
///
/// -- fixture.Inject{T}(value) -------------------------------------------
///    Registers a ready-made object as the single instance for type T.
///    Unlike Freeze (which creates first and then pins), Inject lets you
///    register an object you have already built yourself.
///
/// -- fixture.Customize{T}(c => c.With(...).Without(...)) ----------------
///    Reconfigures the generation rule for a specific type globally.
///    All subsequent Create{T}() calls will follow this rule.
///
/// -- fixture.Build vs fixture.Customize ----------------------------------
///    Build:     one-time configuration for a single Create call.
///    Customize: global configuration, applied to all Create calls for the type.
///
/// -- AutoMoq + ConfigureMembers = true -----------------------------------
///    Methods automatically return default(T) or completed Tasks.
///    This protects against NullReferenceException on unexpected calls.
/// </summary>
public class ChatServiceAutoFixtureTests
{
    // =========================================================================
    // CONCEPT: Comparing manual setup vs AutoFixture setup
    // =========================================================================

    [Fact]
    public void AutoFixture_vs_ManualSetup_Comparison()
    {
        // -- WAY 1: manually (the old approach) --------------------------------
        var uowManual    = new Mock<IUnitOfWork>();
        var eventsManual = new Mock<IChatEvents>();
        var serviceManual = new ChatService(uowManual.Object, eventsManual.Object, NullLogger<ChatService>.Instance);
        // If a new constructor parameter is added -- the test breaks;
        // every test must be updated manually.

        // -- WAY 2: AutoFixture + AutoMoq -------------------------------------
        var fixture = FixtureFactory.Create();
        var serviceAuto = fixture.Create<ChatService>();
        // If a new constructor parameter is added -- the test does NOT break;
        // AutoMoq creates the mock automatically.

        // Both ways produce a working ChatService -- just with different amounts of code
        Assert.NotNull(serviceManual);
        Assert.NotNull(serviceAuto);
    }

    // =========================================================================
    // CONCEPT: fixture.Inject{T}() -- registering a pre-built mock
    // =========================================================================

    [Fact]
    public async Task SendMessageToUserAsync_EmptyContent_ThrowsArgumentException()
    {
        // Arrange ------------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // Inject: registers our pre-configured UoW mock.
        // All mock logic is defined by us, not by AutoFixture.
        var uowMock = new Mock<IUnitOfWork>();
        fixture.Inject(uowMock.Object);

        // IChatEvents will be auto-mocked by AutoMoq (ConfigureMembers = true),
        // so all its methods return completed Tasks by default.
        var service = fixture.Create<ChatService>();

        var me    = fixture.Create<Guid>();
        var other = fixture.Create<Guid>();

        // Act & Assert -------------------------------------------------------------
        // Verify that empty content throws an exception
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendMessageToUserAsync(me, other, "  ", CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendMessageToUserAsync(me, other, "", CancellationToken.None));
    }

    // =========================================================================
    // CONCEPT: Freeze at test level -- granular control
    // =========================================================================

    [Fact]
    public async Task GetMessagesAsync_NotMember_ThrowsUnauthorized()
    {
        // Arrange ------------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // Freeze Mock{IUnitOfWork} -- the same instance will be injected into ChatService
        var uowMock = fixture.Freeze<Mock<IUnitOfWork>>();

        // ChatMemberRepository returns an empty collection ->
        // AnyAsync(cm => ...) returns false -> user is not a chat member
        var emptyMembers = new List<ChatMember>().AsAsyncQueryable();
        var memberRepoMock = new Mock<IRepository<ChatMember>>();
        memberRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>())).Returns(emptyMembers);
        uowMock.Setup(u => u.ChatMemberRepository).Returns(memberRepoMock.Object);

        var service = fixture.Create<ChatService>();
        var me     = fixture.Create<Guid>();
        var chatId = fixture.Create<Guid>();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetMessagesAsync(me, chatId, 50, null));
    }

    // =========================================================================
    // CONCEPT: fixture.Customize{T}() -- global generation rule
    // =========================================================================

    [Fact]
    public void Customize_AllChatMessageDtos_HaveNonEmptyContent()
    {
        // Arrange ------------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // Customize applies to ALL subsequent Create{ChatMessageDto} calls
        // on this fixture. Useful when you need to constrain the value domain.
        fixture.Customize<ChatMessageDto>(c => c
            .With(m => m.Content, "fixed-content")
            .Without(m => m.EditedAt));     // Without -- leaves the property at default (null)

        // Act: generate several -- all should follow the Customize rule
        var messages = fixture.CreateMany<ChatMessageDto>(5).ToList();

        // Assert
        Assert.All(messages, m => Assert.Equal("fixed-content", m.Content));
        Assert.All(messages, m => Assert.Null(m.EditedAt));
    }

    // =========================================================================
    // CONCEPT: Build{T}().OmitAutoProperties() -- only explicit fields
    // =========================================================================

    [Fact]
    public void Build_WithOmitAutoProperties_OnlySetsExplicitFields()
    {
        // Arrange ------------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // OmitAutoProperties() -- AutoFixture leaves all other properties untouched.
        // Useful when the model has many nullable fields and you want to test
        // behaviour for the "minimal" object.
        var minimalMsg = fixture.Build<ChatMessageDto>()
            .OmitAutoProperties()
            .With(m => m.Id,      Guid.NewGuid())
            .With(m => m.Content, "hi")
            .Create();

        // Assert: only explicitly set fields are non-null / non-default
        Assert.Equal("hi", minimalMsg.Content);
        Assert.Equal(Guid.Empty, minimalMsg.ChatId);   // not set -- remains default
        Assert.Null(minimalMsg.EditedAt);
    }

    // =========================================================================
    // CONCEPT: fixture.CreateMany + LINQ -- fast test data set
    // =========================================================================

    [Fact]
    public void CreateMany_ChatListItems_AllHaveUniqueIds()
    {
        // Arrange & Act ------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // CreateMany with no argument creates 3 items (AutoFixture default)
        var items = fixture.CreateMany<ChatListItemDto>().ToList();

        // Assert: AutoFixture guarantees uniqueness of Guid properties
        var ids = items.Select(i => i.Id).Distinct().ToList();
        Assert.Equal(items.Count, ids.Count);
    }

    // =========================================================================
    // CONCEPT: AutoMoq ConfigureMembers = true -- protection from NullRef
    // =========================================================================

    [Fact]
    public void AutoMoq_ConfigureMembers_InterfaceMethodsReturnSafeDefaults()
    {
        // Arrange ------------------------------------------------------------------
        var fixture = FixtureFactory.Create();

        // Obtain an automatically generated mock of IChatEvents
        // WITHOUT any Setup -- ConfigureMembers = true means all interface
        // methods return safe values by default.
        var events = fixture.Create<IChatEvents>();

        // Act & Assert: calling without Setup does NOT throw NullReferenceException
        var task = events.TypingAsync(Guid.NewGuid(), Guid.NewGuid(), true);
        Assert.NotNull(task);       // Task is created, not null
    }
}
