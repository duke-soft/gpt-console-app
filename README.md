# GPT Console App
A simple Chat GPT console interface made with C#.
## Commands:
Run the program to start the chat.
Run it with arguments to enter a prompt: Ex. `./SimpleGPTInterface.exe What is the weather today?`.

Use `-image [prompt]` to generate an image immediately (or see the command below once the program has started).

Use `-code [file]` to add a source code file as a message so you can discuss it with ChatGPT (or see below).
- `[any text]`: Ask ChatGPT.
- `:image` - Ask for a prompt and generate an image.
- `:code` - Ask for a code file and add the contents as a message (could be any file type, really).
- `:chatmodel` - Set the current GPT model ex. gpt-4-32k.
- `:imagemodel` - Set the current image generation model ex. dall-e-3.
- `:sys` - Add a system message ex. "You are a weatherman. Only respond with the weather."
- `:sysview` - View the current system messages.
- `:syswrite` - Write the current system messages to a file.
- `:sysread` - Read in system messages from a file.
- `:sysrm` - Remove a desired system message.
- `:exit` - Exit the program.
  
  ---
  Feel free to rename the file and add it to your system path or alias it so you can easily call it from the command line, kinda like this:
  `gpt whats 4 times 12?`. No "quotes" needed.
