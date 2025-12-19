import { useEffect, useRef, useState } from 'react';

type MessageRole = 'user' | 'assistant';

type ChatMessage = {
  id: string;
  role: MessageRole;
  content: string;
  createdAt: number;
};

function cn(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(' ');
}

function formatTime(ts: number) {
  return new Date(ts).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';

  return (
    <div className={cn('flex', isUser ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[88%] rounded-2xl px-4 py-3 shadow-sm ring-1',
          isUser && 'bg-primary-600 text-white ring-black/5',
          !isUser && 'bg-white text-slate-900 ring-black/5',
        )}
      >
        <div className="whitespace-pre-wrap text-sm leading-6">{message.content}</div>
        <div className={cn('mt-1 text-[11px]', isUser ? 'text-white/80' : 'text-slate-500')}>
          {formatTime(message.createdAt)}
        </div>
      </div>
    </div>
  );
}

export default function ChatWithAI() {
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const listRef = useRef<HTMLDivElement | null>(null);

  const canSend = input.trim().length > 0;

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' });
  }, [messages.length]);

  const send = () => {
    if (!canSend) return;

    const now = Date.now();
    const userMsg: ChatMessage = { id: `m_${now}`, role: 'user', content: input.trim(), createdAt: now };
    setMessages((prev) => [...prev, userMsg]);
    setInput('');

    window.setTimeout(() => {
      const ts = Date.now();
      setMessages((prev) => [
        ...prev,
        {
          id: `m_${ts}`,
          role: 'assistant',
          createdAt: ts,
          content: 'Mình đã nhận yêu cầu.',
        },
      ]);
    }, 250);
  };

  return (
    <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5">
      <div className="border-b border-black/5 px-5 py-4">
        <h2 className="text-lg font-bold text-slate-900">Chat với A.I</h2>
      </div>

      <div ref={listRef} className="h-[420px] overflow-auto px-5 py-5 scrollbar-slim">
        <div className="space-y-3">
          {messages.map((m) => (
            <MessageBubble key={m.id} message={m} />
          ))}
        </div>
      </div>

      <div className="border-t border-black/5 px-5 py-4">
        <div className="flex gap-3">
          <div className="flex-1">
            <textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                  e.preventDefault();
                  send();
                }
              }}
              rows={2}
              className="w-full resize-none rounded-xl border border-black/10 bg-white px-4 py-3 text-sm leading-6 text-slate-900 outline-none ring-0 transition focus:border-primary-300 focus:ring-4 focus:ring-primary-100"
            />
          </div>
          <button
            type="button"
            onClick={send}
            disabled={!canSend}
            className="h-fit rounded-xl bg-primary-600 px-5 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Gửi
          </button>
        </div>
      </div>
    </div>
  );
}
