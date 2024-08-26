﻿using System.ComponentModel;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Samples.Native;

public class TodoPlugin(IServiceProvider serviceProvider)
{
    private const string MemoryModel = "text-embedding-ada-002";
    private const string Collection = "gpt_todos";

    private async Task<ISemanticTextMemory> GetStore()
    {
        var kernelService = serviceProvider.GetRequiredService<KernelService>();
        var store = await kernelService.GetMemoryStore(EmbeddingsModelProvider.OpenAI, MemoryModel);
        return store;
    }

    [KernelFunction("add_todo")]
    [Description("Add a todo to the list")]
    public async Task AddTodo(
        [Description("Todo to add")] string todo
    )
    {
        var store = await GetStore();
        await store.SaveInformationAsync(Collection, todo, Guid.NewGuid().ToString());
    }

    [KernelFunction("complete_todo")]
    [Description("Complete a todo from the list")]
    public async Task<string?> CompleteTodo(
        [Description("Todo to complete")] string todo
    )
    {
        var store = await GetStore();
        var result = store.SearchAsync(Collection, todo, 1, 0.4);

        string? exists = null;
        await foreach (var item in result)
        {
            exists = item.Metadata.Text;
            await store.RemoveAsync(Collection, item.Metadata.Id);
        }

        return exists;
    }

    [KernelFunction("list_todos")]
    [Description("List all todos")]
    public async Task<List<string>> ListTodos()
    {
        var query = """
                    List all my todos
                    """;

        var store = await GetStore();
        var todos = store.SearchAsync(Collection, query, 500, 0.4);

        var todoList = new List<string>();
        await foreach (var todo in todos)
        {
            var text = todo.Metadata.Text;
            todoList.Add(text);
        }

        return todoList;
    }
}