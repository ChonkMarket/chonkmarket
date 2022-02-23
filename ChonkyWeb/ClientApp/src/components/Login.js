import React, { useEffect, useState } from 'react';
import UserNav from './NavMenuElements/UserNav'
import ActionBtn from './NavMenuElements/ActionBtn'
import NavBtn from './NavMenuElements/NavBtn'
import { Button } from '@duik/it';
import { Spinner } from 'reactstrap'
import { useAuth, useClient } from './AuthProvider';
import Discord from '../assets/Discord-Logo+Wordmark-White.png'

function Login(props) {
  const { user, isAuthenticated, isLoading, logout, isPremium } = useAuth();
  const client = useClient();

  const subscribe = async () => {
    const response = await client(`api/stripe/create-checkout-session`)
    let data = response;
    window.stripe.redirectToCheckout({ sessionId: data.SessionId });
  }

  if (isLoading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center' }}>
        <a href='/swagger'>
          <ActionBtn text='API Docs' url='/swagger' />
        </a>
        <NavBtn text='About' url='/aboutus' />
        <div><Spinner size="sm" style={{ color: 'var(--primary)' }} /></div>
      </div>
    )
  } else if (isAuthenticated) {
    return (
      <div style={{ display: 'flex', alignItems: 'center' }}>
        <a href='/swagger'>
          <ActionBtn text='API Docs' url='/swagger' />
        </a>
        <NavBtn text='About' url='/aboutus' />
        <UserNav user={user} logoutAction={logout} />
      </div>
    )
  } else {
    return (
      <div style={{ display: 'flex', alignItems: 'center' }}>
        <a href='/swagger'>
          <ActionBtn text='API Docs' url='/swagger' />
        </a>
        <NavBtn text='About' url='/aboutus' />
        <a href="/Login">
          <Button transparent style={{ border: 'none', height: '2rem', backgroundColor: 'var(--primary)', borderRadius: '100px' }}><span>Login with&nbsp;</span><img src={Discord} alt="Discord" style={{ height: '80%' }} /></Button>
        </a>
      </div>
    )
  }
}
export default Login