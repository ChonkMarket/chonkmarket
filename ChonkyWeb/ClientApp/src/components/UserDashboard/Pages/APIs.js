import React, { useEffect, useState } from 'react';
import { UncontrolledPopover, PopoverBody, Spinner } from 'reactstrap'
import { useClient } from '../../AuthProvider';
import { Button, TextField } from '@duik/it'

const APIs = () => {
  const client = useClient();
  const [apiToken, setApiToken] = useState("");

  useEffect(() => {
    (async function () {
      var token = await client("/get_api_token");
      setApiToken(token);
    })();
  }, [])

  function clipboard() {
    navigator.clipboard.writeText(apiToken);
  }

  return (
    <div>
      <p>All of the documentation for our APIs are located in <a href='/swagger/index.html?urls.primaryName=ChonkMarket' target="_blank">swagger</a>.</p>
      <p>Please reach out via <a href="mailto:team@chonky.market">email</a> or via Discord (ctide#2147) if you have any questions or run into any issues.</p>
      <p>A basic python client is available <a href="https://github.com/ChonkMarket/ChonkPy">on github</a> to use as a starting point to build off of.</p>
      <p>API Tokens will currently only last <b>7 days</b>.</p>
      { apiToken ?
        <TextField disabled placeholder={apiToken} /> :
        <Spinner size="sm" style={{ color: 'var(--primary)', marginBottom: '1rem' }} />}
      <div><Button id="copy-token" onClick={clipboard} transparent style={{ marginRight: '1rem' }}>Copy Token to Clipboard</Button></div>
      <UncontrolledPopover trigger="focus" placement="right" target="copy-token">
        <PopoverBody>Copied!</PopoverBody>
      </UncontrolledPopover>

    </div>)
}

export default APIs