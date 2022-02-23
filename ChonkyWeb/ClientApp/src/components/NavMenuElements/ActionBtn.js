import React from 'react';
import { Button } from '@duik/it'

const ActionBtn = ({text, action, primary=false}) => (
    <Button 
        onClick={action} 
        hidearrows='true'
        transparent='true' 
        style={ primary ? {border: 'none', height: '2rem', backgroundColor: 'var(--primary)', borderRadius: '100px'} : {border: 'none', height: '2rem'}}>
            {text}
    </Button>
)

export default ActionBtn
