import React from 'react';
import { Route, Switch, Redirect } from 'react-router';
import Graph from './components/Graph';
import CandlestickGraph from './components/CandlestickGraph';
import NavMenu from './components/NavMenu';
import UserMain from './components/UserDashboard/UserMain'
import AboutUs from './components/AboutUs/AboutUs'
import Success from './components/StripeCheckout/Success'
import '@duik/it/dist/styles.css'
import './custom.css'
import { AuthProvider, useAuth } from './components/AuthProvider';

function App() {

  const candlestickContents =
    <div id={"container"} style={{ marginTop: '1em' }}>
      <CandlestickGraph />
    </div>

  const contents =
    <div id={"container"} style={{ marginTop: '1em' }}>
      <Graph />
    </div>

  return (
    <AuthProvider>
      <div>
        <NavMenu />
        <Switch>
          <Route exact path='/' render={() => (contents)} />
          <Route path='/graph' render={() => (contents)} />
          <Route path='/candlestickgraph' render={() => (candlestickContents)} />
          <PrivateRoute path='/user'>
            <UserMain />
          </PrivateRoute>
          <Route path='/aboutus' component={AboutUs} />
          <Route path='/checkoutsuccess' component={Success} />
        </Switch>
      </div>
    </AuthProvider>
  )
}

App.displayName = 'App'

export default App

function PrivateRoute({children, ...rest}) {
  let auth = useAuth()
  return (
    <Route
      {...rest}
      render={() => auth.isAuthenticated? (children) : <Redirect to='/' />} />
  )
}