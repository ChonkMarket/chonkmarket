import React from 'react';
import { Button } from '@duik/it'
import { Link } from 'react-router-dom'

const NavBtn = ({text, url, primary=false}) => (
    <Button Component={Link} to={url} hidearrows='true' transparent='true' style={ primary ? {border: 'none', height: '2rem', backgroundColor: 'var(--primary)', borderRadius: '100px'} : {border: 'none', height: '2rem'}}>{text}</Button>
)

export default NavBtn
