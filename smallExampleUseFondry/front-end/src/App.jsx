import { useState } from 'react'
import './App.css'

function App() {
  const [messages, setMessages] = useState([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)

  const sendMessage = async () => {
    if (!input.trim() || loading) return

    const userMessage = { role: 'user', content: input }
    const newMessages = [...messages, userMessage]
    setMessages(newMessages)
    setInput('')
    setLoading(true)

    try {
      // Chuẩn bị history cho API
      const history = newMessages.slice(0, -1).map(msg => ({
        role: msg.role,
        content: msg.content
      }))

      const response = await fetch('http://localhost:5021/api/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          message: input,
          history: history,
          temperature: 0.7,
          maxTokens: 500
        })
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const text = await response.text()
      let data
      try {
        data = JSON.parse(text)
      } catch (e) {
        throw new Error(`Invalid JSON response: ${text.substring(0, 100)}`)
      }

      if (data.error) {
        setMessages([...newMessages, {
          role: 'assistant',
          content: `Lỗi: ${data.error}`
        }])
      } else {
        setMessages([...newMessages, {
          role: 'assistant',
          content: data.message,
          tokensUsed: data.tokensUsed
        }])
      }
    } catch (error) {
      setMessages([...newMessages, {
        role: 'assistant',
        content: `Lỗi kết nối: ${error.message}`
      }])
    } finally {
      setLoading(false)
    }
  }

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      sendMessage()
    }
  }

  return (
    <div className="app">
      <div className="chat-container">
        <div className="chat-header">
          <h1>💬 Chatbot với Azure AI Foundry</h1>
          <p>GPT-4o-mini</p>
        </div>

        <div className="messages-container">
          {messages.length === 0 && (
            <div className="welcome-message">
              <p>👋 Xin chào! Tôi có thể giúp gì cho bạn?</p>
            </div>
          )}
          
          {messages.map((msg, index) => (
            <div key={index} className={`message ${msg.role}`}>
              <div className="message-content">
                {msg.content}
                {msg.tokensUsed && (
                  <span className="token-info">({msg.tokensUsed} tokens)</span>
                )}
              </div>
            </div>
          ))}

          {loading && (
            <div className="message assistant">
              <div className="message-content">
                <div className="typing-indicator">
                  <span></span>
                  <span></span>
                  <span></span>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="input-container">
          <textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="Nhập tin nhắn của bạn..."
            rows={1}
            disabled={loading}
            className="message-input"
          />
          <button
            onClick={sendMessage}
            disabled={!input.trim() || loading}
            className="send-button"
          >
            {loading ? '⏳' : '📤'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default App
