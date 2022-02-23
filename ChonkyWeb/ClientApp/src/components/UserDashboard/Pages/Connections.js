import React from 'react'
import { Card, CardText, CardBody, CardTitle } from 'reactstrap';
import { Button } from '@duik/it'
import { FaDiscord } from 'react-icons/fa'
import ContentTitle from '../../DashboardElements/ContentTitle'
import { useAuth } from '../../AuthProvider';

const Connections = () => {
  const { user } = useAuth();
    return (
        <div>
          <ContentTitle title="CONNECTIONS" />
        <Card>
          <CardBody>
            <CardTitle tag="h5">
              <FaDiscord style={{color: '#6f83d2', fontSize: '2rem', marginRight: '0.5rem'}} />
              <span>{user.name}#{user.discordNameIdentifier}</span></CardTitle>
            <CardText></CardText>
            <Button transparent>Disconnect</Button>
          </CardBody>
        </Card>
      </div>
    )

}

export default Connections