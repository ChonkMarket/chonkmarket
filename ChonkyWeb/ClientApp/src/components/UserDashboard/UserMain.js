import React from 'react'
import { Switch, Route, useRouteMatch, Redirect } from 'react-router';
import { Row, Col, Jumbotron } from 'reactstrap'
import DashboardNav from '.././DashboardElements/DashboardNav'
import styles from './UserDashboard.module.css'
import { Profile, Subscription, Connections, Billings, APIs } from './Pages/index'
import { FaCode, FaUserCircle, FaLayerGroup } from 'react-icons/fa'
import { MdPayment } from 'react-icons/md'
import { FiLink } from 'react-icons/fi'

import { useAuth } from '../AuthProvider';

function UserMain(props) {

  const { user, isAuthenticated, isLoading } = useAuth();

  return (
    <div style={{display: 'flex', justifyContent: 'center'}}>
    <Jumbotron
      className="container"
      style={{
              backgroundColor:'black',
              margin: '3rem',
              paddingTop: '2rem'}}>
        <h1>Profile</h1>
        <h3 style={{color: 'var(--text-tertiary)'}}><strong>username</strong></h3>
        {`${user.name}#${user.discordNameIdentifier}`}
        <h3 style={{color: 'var(--text-tertiary)'}}><strong>email</strong></h3>
        {user.email}
        <hr style={{border: '1px solid var(--primary)', marginTop: '2rem', marginBottom: '2rem'}}/>
        <h1>APIs</h1>
        <APIs />
    </Jumbotron>
  </div> 
  )
}

export default UserMain