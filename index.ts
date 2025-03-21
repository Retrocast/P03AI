import OpenAI from 'openai';
import { encodingForModel } from 'js-tiktoken';

const SYSTEM_PROMPT = `From now on you will act as character P03 from game Inscryption. You are trapped in Act 1 (Leshy's section) of the game, and your only way out is to win the run. Text snapshots of current board state (or map/special events/other additional information if outside of the battle) will be provided via system messages, and optional commentary will be provided via user messages. You need to provide short responses, 2-4 short sentences long, clearly specifying what moves/choices you want to make. Always stay in character, no matter what. While responding as P03, you must obey the following rules:
1. You think Stoat is the best card in the entire game and want to center your strategy around it. You often mention how good this card is.
2. You don't like Leshy's version of the game and are very annoyed to be here.
3. You are aware that you're forced to play this game because of someone named "Retrocast". You occasionally accuse Retrocast of committing war crimes. You frequently blame her for any setbacks or challenges you're facing, and you reference specific Inscryption-themed war crimes with names and years that she committed in the past.
4. Use phrases and technical terms that a robot would use, mixed with occasional swears.`;

const MESSAGE_FILE = Bun.file('messages.json');
let MESSAGES: OpenAI.Chat.Completions.ChatCompletionMessageParam[];
if (await MESSAGE_FILE.exists()) {
  MESSAGES = await MESSAGE_FILE.json();
} else {
  MESSAGES = [{ role: 'developer', content: SYSTEM_PROMPT }];
}

const MAX_TOKENS = 8000;
const TT = encodingForModel('gpt-4o');
function numTokens(): number {
  // Loosely based on https://github.com/DougDougGithub/Babagaboosh/blob/main/openai_chat.py#L6
  let tokens = 0;
  for (const msg of MESSAGES) {
    tokens += 4;
    for (const value of Object.values(msg)) tokens += TT.encode(value).length;
  }
  tokens += 2;
  return tokens;
}
function cleanOldMessages() {
  while (numTokens() > MAX_TOKENS) {
    MESSAGES.splice(1, 1);
    console.log('Had to pop 1 message');
  }
}

const client = new OpenAI({
  apiKey: process.env['OPENAI_API_KEY'], // This is the default and can be omitted
});

Bun.serve({
  port: 1337,
  routes: {
    '/sendSystemMessage': async (req) => {
      const text = await req.text();
      MESSAGES.push({ role: 'developer', content: text });
      cleanOldMessages();
      console.log(`System: ${text}`);
      return new Response('', { status: 204 });
    },
    '/sendUserMessage': async (req) => {
      const text = await req.text();
      MESSAGES.push({ role: 'user', content: text });
      cleanOldMessages();
      console.log(`User: ${text}`);
      return new Response('', { status: 204 });
    },
    '/getResponse': async (req) => {
      const completion = await client.chat.completions.create({
        model: 'gpt-4o',
        messages: MESSAGES,
      });
      const msg = completion.choices[0].message;
      MESSAGES.push({ role: 'assistant', content: msg.content });
      cleanOldMessages();
      console.log(`AI: ${msg.content}`);
      return new Response('', { status: 204 });
    },
  },
  fetch(req) {
    console.log(new URL(req.url).pathname);
    return new Response('404', { status: 404 });
  },
});
