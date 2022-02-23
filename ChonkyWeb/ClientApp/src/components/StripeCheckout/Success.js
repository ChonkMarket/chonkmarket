import React, { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { Jumbotron } from 'reactstrap'
import ActionBtn from '../NavMenuElements/ActionBtn'
import './Success.css'
import logo from './success.png'
import { useClient, useAuth } from '../AuthProvider';

const Success = (props) => {
  const { refreshUser } = useAuth();
  const client = useClient();

  function useQuery() {
    return new URLSearchParams(useLocation().search)
  }

  let sessionId = useQuery().get("sessionId")

  function handleClick(url) {
    props.history.push(url)
  }

  async function stripeSuccess() {
    await client(`api/stripe/success?sessionId=${sessionId}`);
    refreshUser();
  }

  useEffect(() => {
    stripeSuccess()
  }, [])

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'center' }}>
        <Jumbotron
          className="container checkout"
          style={{
            backgroundColor: 'var(--bg-adverse-main)',
            margin: '3rem',
            paddingTop: '2rem',
            paddingBottom: '2rem',
            textAlign: 'center'
          }}>
          <div><img src={logo} style={{ maxHeight: '10vh' }} /></div>
          <div style={{ fontSize: '3vh', fontWeight: 'bold' }}>Subscription Activated</div>
          <div>Thanks for your support! You will receive an email shortly confirming your purchase. </div>
          <div>
            <ActionBtn text="Go to Profile" action={() => handleClick('/user/profile')} primary="true" />
          </div>
          <div>
            <ActionBtn text="Return to Home" action={() => handleClick('/')} />
          </div>
        </Jumbotron>
      </div>
    </>
  )

}

export default Success