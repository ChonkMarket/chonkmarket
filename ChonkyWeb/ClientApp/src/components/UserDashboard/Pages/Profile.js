import React from 'react'
import { FormGroupContainer, FormGroup, TextField, Button } from '@duik/it'

const Profile = ({user}) => {
    return (
        <div>
            <FormGroupContainer horizontal style={{width: '50%'}}>
                <FormGroup>
                    <TextField label="EMAIL" disabled value={user.email}></TextField>
                </FormGroup>
            </FormGroupContainer>
            <FormGroupContainer horizontal style={{width: '50%'}}>
                <FormGroup>
                    <TextField label="PASSWORD" disabled value="********"></TextField>
                </FormGroup>
            </FormGroupContainer>
            {/* <Button primary>Change Password</Button> */}
                    
        </div>

    )
}


export default Profile