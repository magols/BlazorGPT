using Microsoft.SemanticKernel.ChatCompletion;

namespace BlazorGPT.Pipeline;

public static class ChatExtensions
{
	public static ChatHistory ToChatHistory(this Conversation conversation)
	{
		var chatHistory = new ChatHistory();
		foreach (var message in conversation.Messages.Where(c => !string.IsNullOrEmpty(c.Content.Trim())))
		{
			var role =
				message.Role == "system"
					? AuthorRole.System
					: // if the role is system, set the role to system
					message.Role == "user"
						? AuthorRole.User
						: AuthorRole.Assistant;

			chatHistory.AddMessage(role, message.Content);
		}

		return chatHistory;
	}

	public static Conversation ToConversation(this ChatHistory chatHistory)
	{
		var conversation = new Conversation();
		foreach (var message in chatHistory)
		{
			var role =
				message.Role == AuthorRole.System
					? "system"
					: // if the role is system, set the role to system
					message.Role == AuthorRole.User
						? "user"
						: "assistant";

			conversation.AddMessage(new ConversationMessage(role, message.Content));
		}

		return conversation;

	}
}