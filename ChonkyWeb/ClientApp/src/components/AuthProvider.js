import React from 'react';
const localStorageTokenKey = '__token__'
const localStorageUserKey = '__user__'

async function clearCache() {
  window.localStorage.removeItem(localStorageTokenKey)
  window.localStorage.removeItem(localStorageUserKey)
}

const AuthContext = React.createContext()
AuthContext.displayName = 'AuthContext'

function AuthProvider(props) {
  const [token, setToken] = React.useState(getInitialToken())
  const [user, setUser] = React.useState(getInitialUser())
  const [isAuthenticated, setIsAuthenticated] = React.useState(getInitialToken() !== null)
  const [isLoading, setIsLoading] = React.useState(getInitialUser().id === undefined)

  function getInitialToken() {
    return window.localStorage.getItem(localStorageTokenKey)
  }

  function getInitialUser() {
    return JSON.parse(window.localStorage.getItem(localStorageUserKey)) || {}
  }

  async function getToken() {
    let token = window.localStorage.getItem(localStorageTokenKey)
    if (token == undefined) {
      let resp = await fetch('/get_token')
      if (resp.ok) {
        let data = await resp.json()
        if (data.success) {
          token = data.data
          window.localStorage.setItem(localStorageTokenKey, data.data)
        }
      }
    }
    return token;
  }

  const isPremium = React.useMemo(() => user ? user.role === "Subscriber" : false, [user])

  const refreshUser = React.useCallback(() => {
    (async function fetchUser() {
      let resp = await client("/api/identity", { token })
      if (resp) {
        window.localStorage.setItem(localStorageUserKey, JSON.stringify(resp))
        setUser(resp)
        setIsLoading(false)
      }
    })();
  }, [token, user])

  React.useEffect(() => {
    (async function fetchToken() {
      if (token === null) {
        let token = await getToken()
        if (token) {
          setIsAuthenticated(true)
          setToken(token)
        } else {
          setIsLoading(false)
        }
      }
    })();
  }, [])

  React.useEffect(() => {
    if (token) {
      refreshUser();
    }
  }, [token])

  const logout = React.useCallback(async () => {
    await fetch('/session', { method: 'DELETE' });
    clearCache()
    if (window.location.pathname != '/') {
      document.location.href = "/";
    } else {
      setIsAuthenticated(false)
      setUser(null)
    }
  }, [setUser])

  const value = React.useMemo(() => ({ user, logout, token, isAuthenticated, isLoading, isPremium, refreshUser }), [
    user,
    logout,
    token,
    isAuthenticated,
    isLoading,
    isPremium,
    refreshUser
  ])

  return <AuthContext.Provider value={value} {...props} />
}

function useAuth() {
  const context = React.useContext(AuthContext)
  if (context === undefined) {
    throw new Error(`useAuth must be used within a AuthProvider`)
  }
  return context
}

async function client(
  endpoint,
  { data, token, headers: customHeaders, ...customConfig } = {},
) {
  const config = {
    method: data ? 'POST' : 'GET',
    body: data ? JSON.stringify(data) : undefined,
    headers: {
      Authorization: token ? `Bearer ${token}` : undefined,
      'Content-Type': data ? 'application/json' : undefined,
      ...customHeaders,
    },
    ...customConfig,
  }

  if (endpoint[0] != "/")
    endpoint = `/${endpoint}`

  return window.fetch(`${window.location.origin}${endpoint}`, config).then(async response => {
    if (response.status === 401) {
      clearCache();
      if (window.location.pathname != '/')
        document.location.href = "/";
      return Promise.reject({ message: 'Please re-authenticate.' })
    }
    const data = await response.json()
    if (response.ok && data.success) {
      return data.data
    } else {
      return Promise.reject(data)
    }
  })
}

function useClient() {
  const { token } = useAuth()
  return React.useCallback(
    (endpoint, config) => client(endpoint, { ...config, token }),
    [token],
  )
}

export { AuthProvider, useAuth, useClient }