import React, { useEffect } from 'react'
import { Card, CardText, CardBody, CardTitle } from 'reactstrap';
import { Button } from '@duik/it'
import { BsCheckBox } from 'react-icons/bs'
import ContentTitle from '../../DashboardElements/ContentTitle'
import { useAuth, useClient } from '../../AuthProvider';

const Subscriptions = () => {
  const { isPremium, isLoading, user } = useAuth();
  const client = useClient();
  const billingManagement = async () => {
    const data = await client(`api/stripe/billing`, { method: "POST" });
    if (data.url)
      window.location = data.url;
  }

  if (isLoading)
    return (<div></div>)

  return (
    <div>
      <ContentTitle title="SUBSCRIPTION" />
      <Card>
        <CardBody>
          <CardTitle tag="h5">
            <span>Support the Chonk Market</span>
          </CardTitle>
          {!isPremium && (
            <>
              <CardText>
                Right now we have data back to 3/19 for a handful of tickers. Historical data is expensive, and getting full historical option chain state throughout the day is even more expensive. Subscription premiums are there to fund the acquisition of this data and, ideally, to support a broader range of tickers than just the few we are ingesting today. Subscribers also will get early access to new functionality as we make it available. We have a handful of ideas in the works that will hopefully make your life as a trader better!
              </CardText>
              <Button primary>Subscribe</Button>
            </>
          )}
          {isPremium && (
            <>
              <CardText>
                Thank you so much for your support! It's greatly appreciated. If you would like to update your billing options, or cancel your subscription, you can do so via the button below.
              </CardText>
              <Button primary onClick={billingManagement}>Manage Billing</Button>
            </>
          )}
        </CardBody>
      </Card>
    </div >
  )
}

export default Subscriptions