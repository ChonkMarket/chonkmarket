import React from 'react'
import { Switch, Route, useRouteMatch } from 'react-router';
import { Row, Col } from 'reactstrap'
import DashboardNav from '.././DashboardElements/DashboardNav'
import styles from './UserDashboard.module.css'
import { Profile, Subscription, Connections, Billings, APIs } from './Pages/index'
import { FaCode, FaUserCircle, FaLayerGroup } from 'react-icons/fa'
import { MdPayment } from 'react-icons/md'
import { FiLink } from 'react-icons/fi'

function UserDashboard() {

    let { path, url } = useRouteMatch();

    const subpaths = [
      {
        path: 'profile',
        display: 'Profile',
        component: <Profile />,
        icon: <FaUserCircle />
      },
      {
        path: 'connections',
        display: 'Connections',
        component: <Connections />,
        icon: <FiLink />
      },
      //{
      //  path: 'billings',
      //  display: 'Billings',
      //  component: <Billings />,
      //  icon: <MdPayment />
      //},
      {
        path: 'subscription',
        display: 'Subscription',
        component: <Subscription />,
        icon: <FaLayerGroup />
      },
      {
        path: 'apis',
        display: 'APIs',
        component: <APIs />,
        icon: <FaCode />
      },
    ]

  return (
      <>
      <Switch>
        <Route exact path={path} render={() => ( <div>something</div> )} />
        {subpaths.map(subpath => (
          <Route exact path={`${path}/${subpath.path}`} key={subpath.path} render={() => ( 
            <div className="container">
              <Row>
                <Col sm="3" 
                className={styles.panel} 
                style={{paddingLeft: '0px', paddingRight: '0px', maxWidth: '300px'}}>
                  <DashboardNav subpaths={subpaths} active={subpath.path} url={url} />
                </Col>
                <Col sm="9">
                  <div className="container">
                  {subpath.component}
                  </div>
                </Col>
              </Row>
            </div>
          )} />
        ))}
      </Switch>
      </>
  )
}

export default UserDashboard