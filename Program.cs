using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

/* 
 * SimpleGPTInterface
 * 
 * Console application that takes a prompt and calls the OpenAI API to give
 * a response. Can hold a conversation by storing chat messages and the
 * model can be changed. API key is read from OPENAI_API_KEY environment
 * variable.
 * 
 * Commands:
 * :exit        - exit the program
 * :sys         - add a system message to alter behavior
 * :sysview     - view all current system messages
 * :sysrm       - remove a given system message
 * :syswrite    - write current system messages to given file
 * :sysread     - load system messages from given file
 * :image       - generate an image from a given prompt
 * :chatmodel   - set chat model
 * :imagemodel  - set image model
 * [any string] - respond to user input
 * 
 * Written by Luke Dykstra
 * 03/28/2024
 */
namespace SimpleGPTInterface {
    class Program {
        static string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        static async Task Main(string[] args) {
            // List to store chat message objects for current conversation
            List<Message> messages    = new List<Message>();
            // Lists of available chat and image models
            List<string> image_models = new List<string>{ "dall-e-3", "dall-e-2" };
            List<string> chat_models  = new List<string>{"gpt-3.5-turbo", "gpt-4", "gpt-4-turbo-preview", "gpt-4-vision-preview", "gpt-4-32k",
                                                         "gpt-3.5-turbo-16k"};
            // Current chat and image model being used
            string chat_model  = "gpt-3.5-turbo";
            string image_model = "dall-e-3";
            string prompt;

            int[] indices;
            bool running = true;

            if (args.Length > 0) {
                if (args[0] == "-image") {
                    args[0] = "";
                    Console.WriteLine("Generating...");
                    // Send prompt and model to image generation API and await response
                    string url = await GetImageGenerationAsync(String.Join(" ", args), "dall-e-3");
                    // Open URL to generated image in default browser
                    System.Diagnostics.Process.Start(url);
                } else {
                    // Add a user message and respond to it
                    messages.Add(new Message("user", String.Join(" ", args)));
                    // Send messages to chat completion API and await response
                    string response = await GetChatCompletionAsync(messages.ToArray(), chat_model);
                    Console.WriteLine(response);
                    // Add AI response to messages
                    messages.Add(new Message("assistant", response));
                }
            }

            while (running) {
                // Ask the user for a prompt and add it to the list of messages
                Console.Write($"{chat_model}> ");
                prompt = Console.ReadLine();

                switch(prompt) {
                    case ":exit":
                        // Stop the program
                        running = false;
                        break;
                    case ":sys":
                        // Add a system message
                        Console.Write("Enter system message: ");
                        prompt = Console.ReadLine();

                        if (prompt != "") {
                            messages.Add(new Message("system", prompt));
                        }

                        break;
                    case ":sysview":
                        ListMessages(messages, "system");
                        break;
                    case ":sysrm":
                        indices = ListMessages(messages, "system");

                        if (indices.Length > 0) {
                            Console.Write("Enter # of message to remove: ");
                            int choice = int.Parse(Console.ReadLine());

                            if(choice > 0 && choice <= indices.Length) {
                                messages.RemoveAt(indices[choice - 1]);
                                Console.WriteLine("System message removed.");
                            } else {
                                Console.WriteLine("List number out of bounds.");
                            }
                        } else {
                            Console.WriteLine("No system messages to remove.");
                        }

                        break;
                    case ":syswrite":
                        indices = ListMessages(messages, "system", false);

                        if (indices.Length > 0) {
                            Console.Write("Enter file name: ");
                            prompt = Console.ReadLine();

                            // Clear contents of the file
                            File.WriteAllText(prompt, "");
                            StreamWriter streamWriter = File.AppendText(prompt);

                            // For each system message, append its contents to the file
                            foreach (int i in indices) {
                                streamWriter.WriteLine(messages[i].content);
                            }

                            streamWriter.Close();
                            Console.WriteLine($"System messages saved to file \'{prompt}\'.");
                        } else {
                            Console.WriteLine("No system messages to save.");
                        }

                        break;
                    case ":sysread":
                        Console.Write("Enter file name: ");
                        prompt = Console.ReadLine();
                        string[] lines = File.ReadAllLines(prompt);

                        if (lines.Length > 0) {
                            // For each line found in the file, add it as a new system message
                            foreach (string line in lines) {
                                messages.Add(new Message("system", line));
                            }

                            Console.WriteLine($"System messages loaded from file \'{prompt}\'.");
                        } else {
                            Console.WriteLine("File empty.");
                        }

                        break;
                    case ":image":
                        Console.Write($"{image_model}>: ");
                        prompt = Console.ReadLine();

                        if (prompt != "") {
                            Console.WriteLine("Generating...");

                            // Send prompt and model to image generation API and await response
                            string url = await GetImageGenerationAsync(prompt, "dall-e-3");
                            // Open URL to generated image in default browser
                            System.Diagnostics.Process.Start(url);
                        }

                        break;
                    case ":chatmodel":
                        Console.Write("Enter chat model: ");
                        prompt = Console.ReadLine();

                        if (chat_models.Contains(prompt)) {
                            chat_model = prompt;
                        } else {
                            Console.WriteLine("Model not available.");
                        }

                        break;
                    case ":imagemodel":
                        Console.Write("Enter image model: ");
                        prompt = Console.ReadLine();

                        if (image_models.Contains(prompt)) {
                            image_model = prompt;
                        }
                        else {
                            Console.WriteLine("Model not available.");
                        }

                        break;
                    default:
                        // Add a user message and respond to it
                        messages.Add(new Message("user", prompt));

                        // Send messages to chat completion API and await response
                        string response = await GetChatCompletionAsync(messages.ToArray(), chat_model);
                        Console.WriteLine(response);

                        // Add AI response to messages
                        messages.Add(new Message("assistant", response));
                        break;
                } 
            }
        }

        static async Task<string> GetChatCompletionAsync(Message[] messages, string model) {
            // Create and use new temporary HTTP client
            using (HttpClient client = new HttpClient()) {
                // Add Authorization: Bearer [api key] to the HTTP header
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Create an object for the request body with given message
                // and model properties
                var requestBody = new {
                    messages = messages,
                    model = model
                };

                // Create the message to send to the API endpoint by JSON
                // serializing the requestBody object
                var message = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                // Post request to API and await a response
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", message);
                // Throw an error if response is not successful
                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON and get the message string from the object
                dynamic responseBody = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                return responseBody.choices[0].message.content;
            }
        }

        static async Task<string> GetImageGenerationAsync(string prompt, string model) {
            // Create and use new temporary HTTP client
            using (HttpClient client = new HttpClient()) {
                // Add Authorization: Bearer [api key] to the HTTP header
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Create an object for the request body with given message
                // and model properties
                var requestBody = new {
                    prompt = prompt,
                    model = model
                };

                // Create the message to send to the API endpoint by JSON
                // serializing the requestBody object
                var message = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                // Post request to API and await a response
                var response = await client.PostAsync("https://api.openai.com/v1/images/generations", message);
                // Throw an error if response is not successful
                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON and get the message string from the object
                dynamic responseBody = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                return responseBody.data[0].url;
            }
        }

        /*
         * Given a List<Message> and a role as a string, print out the messages in the
         * list from that role. Set role to 'all' to print all messages.
         * Set argument print to false to not print any output.
         * 
         * Returns an int array of the found indices of the messages in the message List.
         */
        static int[] ListMessages(List<Message> messages, string role, bool print = true) {
            List<int> indices = new List<int>();
            Message msg;
            int listNum = 1;

            for(int i = 0; i < messages.Count; i ++) {
                msg = messages[i];

                if (msg.role == role || role == "all") {
                    if (print) {
                        Console.WriteLine($"{listNum++}: {msg.content}");
                    }

                    indices.Add(i);
                }
            }

            return indices.ToArray();
        }

        /* 
         * JSON-Serializable class for storing messages with role and content fields
         */
        public class Message {
            [JsonProperty("role")]
            public string role = "user";

            [JsonProperty("content")]
            public string content = "";

            public Message(string role, string content) {
                this.role = role;
                this.content = content;
            }
        }
    }
}
