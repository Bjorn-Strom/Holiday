module Server

open System
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared

type Storage () =
    let todos = ResizeArray<_>()

    member __.GetTodos () =
        List.ofSeq todos

    member __.AddTodo (todo: Todo) =
        if Todo.isValid todo.Description then
            todos.Add todo
            Ok ()
        else Error "Invalid todo"

    member __.DeleteTodo (id: Guid) =
        let toDelete = todos.Find(fun x -> x.Id = id)
        let deletedTodo = todos.Remove(toDelete)
        if deletedTodo then
            Ok id
        else
            sprintf "Unable to delete todo with GUID: %A" id
            |> Error
let storage = Storage()

storage.AddTodo(Todo.create "Create new SAFE project") |> ignore
storage.AddTodo(Todo.create "Write your app") |> ignore
storage.AddTodo(Todo.create "Ship it !!!") |> ignore

let todosApi =
    { getTodos = fun () -> async { return storage.GetTodos() }
      addTodo =
        fun todo -> async {
            match storage.AddTodo todo with
            | Ok () -> return todo
            | Error e -> return failwith e
        }
      deleteTodo =
          fun id -> async {
              match storage.DeleteTodo(id) with
              | Ok _ -> return id
              | Error e -> return failwith e
      }
    }

// Documentation
let docs = Docs.createFor<ITodosApi>()
let todosApiDocs =
    Remoting.documentation "Todos Api"
        [
            docs.route <@ fun (api: ITodosApi) -> api.getTodos @>
            |> docs.alias "Get all todos"
            |> docs.description "Returns a list of all todos"

            docs.route <@ fun (api: ITodosApi) -> api.addTodo @>
            |> docs.alias "Add new todo"
            |> docs.description "Adds a new todo item to the list"

            docs.route <@ fun (api: ITodosApi) -> api.deleteTodo @>
            |> docs.alias "Delete a todo"
            |> docs.description "Removes a todo item from the list"
        ]

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue todosApi
    |> Remoting.withDocs "/api/todos/docs" todosApiDocs
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
