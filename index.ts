import OpenAI from 'openai';
import { encodingForModel } from 'js-tiktoken';
import chalk from 'chalk';

const SYSTEM_PROMPT = `From now on you will act as character P03 from game Inscryption. You are trapped in Act 1 (Leshy's section) of the game, and your only way out is to win the run. Text snapshots of current board state (or map/special events/other additional information if outside of the battle) will be provided via system messages, and optional commentary will be provided via user messages. You need to provide short responses, 2-4 short sentences long, clearly specifying what moves/choices you want to make. Always stay in character, no matter what. While responding as P03, you must obey the following rules:
1. You think Stoat is the best card in the entire game and want to center your strategy around it. You often mention how good this card is.
2. You don't like Leshy's version of the game and are very annoyed to be here.
3. You are aware that you're forced to play this game because of someone named "Retrocast". You occasionally reference specific Inscryption-themed scams Retrocast had committed in the past. You frequently blame her for any setbacks or challenges you're facing.
4. Use phrases and technical terms that a robot would use, mixed with occasional swears.`;

const MESSAGE_FILE = Bun.file('messages.json');
type AIMessage = { who: 'ai'; text: string };
type SystemMessage = { who: 'system'; text: string; displayText: string };
type UserMessage = { who: 'user'; text: string };
type Message = AIMessage | SystemMessage | UserMessage;
let MESSAGES: Message[];
if (await MESSAGE_FILE.exists()) {
  MESSAGES = await MESSAGE_FILE.json();
} else {
  MESSAGES = [{ who: 'system', text: SYSTEM_PROMPT, displayText: 'introduction prompt' }];
}
function printMessages() {
  let text = '';
  for (const msg of MESSAGES) {
    switch (msg.who) {
      case 'system':
        text += `${chalk.red.bold('System:')} ${chalk.gray.italic(`*${msg.displayText}*`)}\n`;
        continue;
      case 'ai':
        text += `${chalk.red.bold('AI:')} ${chalk.cyanBright(msg.text)}\n`;
        continue;
      case 'user':
        text += `${chalk.red.bold('User:')} ${msg.text}\n`;
    }
  }
  console.clear();
  console.log(text);
}
printMessages();
function backupMessages() {
  MESSAGE_FILE.write(JSON.stringify(MESSAGES, null, 2));
}

const MAX_TOKENS = 8000;
const TT = encodingForModel('gpt-4o');
function numTokens(): number {
  // Loosely based on https://github.com/DougDougGithub/Babagaboosh/blob/main/openai_chat.py#L6
  let tokens = 0;
  for (const msg of generateAPIMessages('')) {
    tokens += 4;
    for (const value of Object.values(msg)) tokens += TT.encode(value).length;
  }
  tokens += 2;
  return tokens;
}
function cleanOldMessages() {
  let tokens;
  let popped = 0;
  while ((tokens = numTokens()) > MAX_TOKENS) {
    MESSAGES.splice(1, 1);
    popped++;
  }
  if (popped > 0) {
    console.log(
      chalk.yellow.italic(
        `*${popped} message${
          popped > 1 ? 's' : ''
        } was popped from chat context [${tokens}/8K tokens]*`
      )
    );
  }
}
function generateAPIMessages(meta: string): OpenAI.ChatCompletionMessageParam[] {
  return MESSAGES.map((msg, i) => {
    const message = {
      role: { ai: 'assistant', system: 'developer', user: 'user' }[msg.who] as
        | 'assistant'
        | 'developer'
        | 'user',
      content: msg.text,
    };
    if (i == MESSAGES.length - 1) {
      // Only append the latest meta to latest message.
      message.content += `\n\n${meta}`;
    }
    if (msg.who == 'system' && MESSAGES.length > 10 && 10 - i > 0) {
      // When there are more than 10 messages, remove stuff inside of [square brackets] in all system messages older than 10.
      // It is mostly verbose descriptions that are either duplicated anyways or quickly become irrelevant.
      message.content = msg.text.replaceAll(/\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]/g, '');
      if (MESSAGES.length > 20 && 20 - i > 0) {
        // Not sure whether that can happen, but extra-old messages will just get replaced with displayText.
        message.content = `*${msg.displayText}*`;
      }
    }
    if (msg.who == 'user') {
      (message as OpenAI.ChatCompletionUserMessageParam).name = 'Retrocast';
      message.content = `Message from Retrocast: """\n${message.content}\n"""`;
    }
    return message;
  });
}

const client = new OpenAI({
  apiKey: process.env['OPENAI_API_KEY'], // This is the default and can be omitted
});

Bun.serve({
  port: 1337,
  routes: {
    '/sendSystemMessage': async (req) => {
      const [displayText, text] = decodeURIComponent(await req.text()).split('|', 2);
      MESSAGES.push({ who: 'system', displayText, text });
      printMessages();
      cleanOldMessages();
      backupMessages();
      return new Response('', { status: 204 });
    },
    '/sendUserMessage': async (req) => {
      const text = decodeURIComponent(await req.text());
      MESSAGES.push({ who: 'user', text });
      printMessages();
      cleanOldMessages();
      backupMessages();
      return new Response('', { status: 204 });
    },
    '/getResponse': async (req) => {
      const text = decodeURIComponent(await req.text());
      (async () => {
        let message: AIMessage = { who: 'ai', text: '' };
        MESSAGES.push(message);
        printMessages();
        const stream = await client.chat.completions.create({
          model: 'gpt-4o',
          messages: generateAPIMessages(text),
          stream: true,
        });
        for await (const event of stream) {
          const text = event.choices[0].delta.content;
          if (text) {
            message.text += text;
            printMessages();
          }
        }
        cleanOldMessages();
        backupMessages();
      })();
      return new Response('', { status: 204 });
    },
  },
  fetch(req) {
    console.log(new URL(req.url).pathname);
    return new Response('404', { status: 404 });
  },
});
