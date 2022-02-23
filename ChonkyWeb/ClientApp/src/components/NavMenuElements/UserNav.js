import React from 'react';
import { Link } from 'react-router-dom'
import { Dropdown, DropdownItem, Button } from '@duik/it'
import { withRouter } from "react-router";
import { useAuth } from '../AuthProvider';

const ImageButton = ({
  handleToggle, handleClose, handleOpen, setOpenState, isOpen
}) => {
  const { user } = useAuth();
  return (
    <Button onClick={handleToggle} className="btn btn-transparent" style={{ "border": "none", "height": "2rem" }}>
      {user.avatarUrl && <img style={{ height: "32px", "borderRadius": "16px" }} src={user.avatarUrl} />}
      <span style={{ "marginLeft": "5px" }}>{user.name}</span>
    </Button>
  )
}

const UserNav = ({ user, logoutAction }) => {
  return (
    <Dropdown
      ButtonComponent={ImageButton}
      closeOnOptionClick={true}
      buttonProps={{
        hideArrows: true,
        transparent: true,
        style: { border: 'none', height: '2rem' },
      }}>
      <DropdownItem Component={Link} to={'/user/profile'}>Profile</DropdownItem>
      <DropdownItem onClick={logoutAction}>Log Out</DropdownItem>
    </Dropdown >)
}

export default withRouter(UserNav);